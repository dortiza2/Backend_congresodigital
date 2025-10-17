#!/usr/bin/env node
/*
  Limpieza agresiva: Mueve archivos y carpetas NO esenciales a `trash/` y
  genera `trash_report.md`. Mantiene solo lo estrictamente necesario para correr
  el backend (Congreso.Api) y el frontend (landing), además de DB migrations/security.

  Reglas principales:
  - Mover: archivos con extensiones .md, .txt, .http, .sh, .sql excepto en db/migrations y db/security
  - Mover: carpetas conocidas no esenciales: docs/, tests/, landing/docs/, landing/tests/, imagenesausar/, HashGenerator/, TestBCrypt/, .vscode/
  - Mantener: Congreso.Api/** (código), landing/** (código y configuración esencial), db/migrations/**, db/security/**, tools/**, node_modules/**, .gitignore, package.json, package-lock.json, tsconfig/next configs.
  - No tocar: trash/, __trash__/, _trash/

  Advertencia: este proceso es destructivo a nivel de estructura (renames). Revise `trash/` y
  `trash_report.md` antes de eliminación definitiva.
*/

const fs = require('fs');
const path = require('path');

const root = process.cwd();
const trashDir = path.join(root, 'trash');

async function statSafe(p) { try { return await fs.promises.stat(p); } catch { return null; } }

async function* walk(dir) {
  const entries = await fs.promises.readdir(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    const rel = path.relative(root, full);
    if (!rel || rel === '.') continue;
    const lower = rel.toLowerCase();
    // Ignorar recipientes de basura
    if (lower.startsWith('trash/') || lower.startsWith('__trash__/') || lower.startsWith('_trash/')) continue;
    // Ignorar node_modules
    if (lower.startsWith('node_modules/')) continue;
    yield { full, rel, entry: e };
    if (e.isDirectory()) {
      yield* walk(full);
    }
  }
}

const nonEssentialDirs = [
  'docs/', 'tests/', '.vscode/', 'imagenesausar/', 'hashgenerator/', 'testbcrypt/',
  'landing/docs/', 'landing/tests/'
];

const essentialDirKeeps = [
  'congreso.api/', 'landing/', 'db/migrations/', 'db/security/', 'tools/'
];

const nonEssentialExts = new Set(['.md', '.txt', '.http', '.sh', '.sql']);

function isDirNonEssential(relLower) {
  return nonEssentialDirs.some(p => relLower.startsWith(p));
}

function isUnderKeep(relLower) {
  return essentialDirKeeps.some(p => relLower.startsWith(p));
}

function shouldMoveFile(rel) {
  const lower = rel.toLowerCase();
  const ext = path.extname(lower);
  // Move by extension if non-essential
  if (nonEssentialExts.has(ext)) {
    // exceptions: keep .sql under db/migrations or db/security
    if (ext === '.sql') {
      if (lower.startsWith('db/migrations/') || lower.startsWith('db/security/')) return false;
    }
    // keep essential configs even if .md? No, move .md regardless (docs)
    // within Congreso.Api or landing we still move .md/.http/.sh files
    return true;
  }
  // Move test/config artifacts in tests directories
  if (lower.includes('/tests/')) return true;
  // Otherwise keep
  return false;
}

function contentType(rel) {
  const lower = rel.toLowerCase();
  if (lower.endsWith('.md')) return 'documentación';
  if (lower.endsWith('.txt')) return 'texto plano';
  if (lower.endsWith('.http')) return 'tests http';
  if (lower.endsWith('.sh')) return 'script de shell';
  if (lower.endsWith('.sql')) return 'script SQL';
  if (lower.includes('/tests/')) return 'tests';
  return 'no esencial';
}

function dirReason(rel) {
  return 'Carpeta no esencial (documentación, tests o recursos no críticos).';
}

function fileReason(rel) {
  const type = contentType(rel);
  if (rel.toLowerCase().startsWith('db/migrations/') || rel.toLowerCase().startsWith('db/security/')) {
    return 'Esencial para DB (migrations/security)';
  }
  return `Archivo ${type}; no requerido para ejecución básica del proyecto.`;
}

async function ensureDir(p) { await fs.promises.mkdir(p, { recursive: true }); }

async function movePathPreserveStructure(rel) {
  const src = path.join(root, rel);
  const dest = path.join(trashDir, rel);
  await ensureDir(path.dirname(dest));
  const exists = await statSafe(dest);
  if (exists) {
    const base = path.parse(dest);
    const alt = path.join(base.dir, `${base.name}.${Date.now()}${base.ext}`);
    await fs.promises.rename(src, alt);
    return path.relative(root, alt);
  } else {
    await fs.promises.rename(src, dest);
    return path.relative(root, dest);
  }
}

async function main() {
  await ensureDir(trashDir);
  const moved = [];
  const movedDirs = new Set();

  // Primero mover carpetas claramente no esenciales
  for (const nd of nonEssentialDirs) {
    const d = path.join(root, nd.replace(/\/$/, ''));
    const st = await statSafe(d);
    if (st && st.isDirectory()) {
      const rel = nd.replace(/\/$/, '');
      const newRel = await movePathPreserveStructure(rel);
      moved.push({ kind: 'dir', name: path.basename(rel), original: rel, newLocation: newRel, type: 'carpeta', reason: dirReason(rel) });
      movedDirs.add(rel.toLowerCase());
      console.log(`Movida carpeta: ${rel} -> ${newRel}`);
    }
  }

  // Para el resto, mover archivos no esenciales por extensión/patrón
  for await (const { rel, entry } of walk(root)) {
    const lower = rel.toLowerCase();
    if (movedDirs.has(lower)) continue; // ya movida como carpeta
    if (entry.isDirectory()) {
      // evitar mover carpetas esenciales
      continue;
    }
    if (shouldMoveFile(rel)) {
      // Excepciones: mantener en paths esenciales si no coincide extensiones
      if (lower.startsWith('db/migrations/') || lower.startsWith('db/security/')) continue;
      const newRel = await movePathPreserveStructure(rel);
      moved.push({ kind: 'file', name: path.basename(rel), original: rel, newLocation: newRel, type: contentType(rel), reason: fileReason(rel) });
      console.log(`Movido: ${rel} -> ${newRel}`);
    }
  }

  // Generar reporte
  let md = '';
  md += '## Reporte de Archivos para Eliminación\n\n';
  for (const m of moved) {
    md += `### ${m.name}\n`;
    md += `- **Ubicación original**: ${m.original}\n`;
    md += `- **Tipo**: ${m.type}\n`;
    md += `- **Razón**: ${m.reason}\n\n`;
  }
  await fs.promises.writeFile(path.join(root, 'trash_report.md'), md, 'utf8');
  console.log(`Reporte generado: ${path.join(root, 'trash_report.md')}`);
}

main().catch(err => { console.error('Error en limpieza agresiva:', err); process.exit(1); });