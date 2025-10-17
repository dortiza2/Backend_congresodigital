#!/usr/bin/env node
// Detecta archivos potencialmente no referenciados a partir de puntos de entrada.
// No elimina nada: genera reporte JSON y lista candidatos.

const fs = require('fs');
const path = require('path');

const projectRoot = path.resolve(__dirname, '..');
const landingRoot = path.join(projectRoot, 'landing');
const apiRoot = path.join(projectRoot, 'Congreso.Api');

// Entrypoints conocidos
const entrypoints = [
  path.join(landingRoot, 'pages', '_app.tsx'),
  path.join(landingRoot, 'pages', 'index.tsx'),
  path.join(apiRoot, 'Program.cs')
];

// Extensiones a considerar
const codeExts = ['.ts', '.tsx', '.js', '.jsx', '.cs'];

// Regex para importaciones básicas (TS/JS)
const importRegex = /import\s+[^'";]+from\s+['"]([^'"]+)['"];?|require\(['"]([^'"]+)['"]\)/g;
// Regex simple para using en C# y namespaces, y referencias a archivos del proyecto no robusto
const csRegex = /using\s+[\w\.]+;|new\s+[A-Z][\w]+\(/g;

function listFilesRecursive(dir, acc = []) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      // saltar node_modules y bin/obj
      if (e.name === 'node_modules' || e.name === '.next' || e.name === 'bin' || e.name === 'obj') continue;
      acc = listFilesRecursive(full, acc);
    } else {
      acc.push(full);
    }
  }
  return acc;
}

function isCodeFile(file) {
  return codeExts.includes(path.extname(file));
}

function resolveImport(fromFile, spec) {
  // Resolver rutas relativas del front
  if (spec.startsWith('./') || spec.startsWith('../')) {
    const base = path.dirname(fromFile);
    const abs = path.resolve(base, spec);
    // Probar con extensiones
    const candidates = [abs, ...['.ts', '.tsx', '.js', '.jsx'].map(ext => abs + ext)];
    for (const c of candidates) {
      if (fs.existsSync(c)) return c;
    }
  }
  // Aliases del front: '@/'
  if (spec.startsWith('@/')) {
    const abs = path.join(landingRoot, spec.replace(/^@\//, ''));
    const candidates = [abs, ...['.ts', '.tsx', '.js', '.jsx'].map(ext => abs + ext)];
    for (const c of candidates) {
      if (fs.existsSync(c)) return c;
    }
  }
  // Import estático de public (no archivo de código)
  if (spec.startsWith('/')) {
    const p = path.join(landingRoot, 'public', spec.slice(1));
    if (fs.existsSync(p)) return p;
  }
  return null;
}

function scanFile(file) {
  const refs = new Set();
  const content = fs.readFileSync(file, 'utf8');
  if (file.endsWith('.cs')) {
    // Muy básico: no resolvemos dependencias C#, solo marcamos que Program.cs existe
    return refs;
  }
  let match;
  while ((match = importRegex.exec(content)) !== null) {
    const spec = match[1] || match[2];
    if (!spec) continue;
    const resolved = resolveImport(file, spec);
    if (resolved) refs.add(resolved);
  }
  return refs;
}

function buildGraph() {
  const graph = new Map();
  const visited = new Set();
  const queue = [...entrypoints.filter(f => fs.existsSync(f))];

  while (queue.length) {
    const file = queue.shift();
    if (!file || visited.has(file)) continue;
    visited.add(file);
    if (!isCodeFile(file)) continue;
    const refs = scanFile(file);
    graph.set(file, refs);
    for (const r of refs) {
      if (!visited.has(r) && isCodeFile(r)) {
        queue.push(r);
      }
    }
  }
  return { graph, visited };
}

function main() {
  const allFiles = listFilesRecursive(projectRoot);
  const { visited } = buildGraph();
  const codeFiles = allFiles.filter(isCodeFile);

  const unreferenced = codeFiles.filter(f => !visited.has(f));

  const report = {
    generatedAt: new Date().toISOString(),
    entrypoints,
    totalFiles: allFiles.length,
    codeFiles: codeFiles.length,
    referencedFiles: visited.size,
    unreferencedCount: unreferenced.length,
    unreferencedFiles: unreferenced,
  };

  const outDir = path.join(projectRoot, 'docs', 'cleanup');
  fs.mkdirSync(outDir, { recursive: true });
  const outPath = path.join(outDir, 'dead_files_report.json');
  fs.writeFileSync(outPath, JSON.stringify(report, null, 2));

  console.log(`Reporte generado: ${outPath}`);
  console.log(`Archivos de código sin referencia (top 20):`);
  unreferenced.slice(0, 20).forEach(f => console.log(' -', path.relative(projectRoot, f)));
}

if (require.main === module) {
  main();
}