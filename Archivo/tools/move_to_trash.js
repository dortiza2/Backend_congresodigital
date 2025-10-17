#!/usr/bin/env node
/*
  Mueve archivos de texto plano no críticos a la carpeta `trash` y genera `trash_report.md`.
  Criterios:
  - Solo archivos de texto: .md, .txt, .http
  - Ubicados en carpetas de documentación (docs/) o con nombres típicos de auditoría/análisis en la raíz o subproyectos
  - Excluye elementos potencialmente críticos como ENV_VARIABLES_GUIDE.md
  - No toca código fuente, migraciones, datos, imágenes ni binarios
*/

const fs = require('fs');
const path = require('path');

const root = process.cwd();
const trashDir = path.join(root, 'trash');

/** Utilidades **/
async function statSafe(p) {
  try { return await fs.promises.stat(p); } catch { return null; }
}

async function* walk(dir) {
  const entries = await fs.promises.readdir(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    const rel = path.relative(root, full);
    if (rel === '' || rel === '.') continue;
    // Ignorar rutas que no debemos tocar
    if (e.isDirectory()) {
      const lower = rel.toLowerCase();
      if (lower.startsWith('.git') || /\b(node_modules|bin|obj)\b/.test(lower)) continue;
      if (lower.startsWith('trash/') || lower.startsWith('_trash/') || lower.startsWith('__trash__/')) continue;
      yield { full, rel, entry: e };
      yield* walk(full);
    } else {
      yield { full, rel, entry: e };
    }
  }
}

const allowedExt = new Set(['.md', '.txt', '.http']);
const forbiddenNames = new Set(['env_variables_guide.md']);

function isTextCandidate(rel) {
  const lower = rel.toLowerCase();
  const ext = path.extname(lower);
  if (!allowedExt.has(ext)) return false;
  if ([...forbiddenNames].some(n => lower.endsWith('/' + n) || lower === n)) return false;

  const inDocs = lower.startsWith('docs/') || lower.includes('/docs/');
  const inLandingDocs = lower.startsWith('landing/docs/');
  const isRootDoc = !lower.includes('/') && /(audit|auditoria|analisis|resultado|performance|qa|reporte|informe|pendientes|plan|solicitud|inventario|alineacion|matriz|validacion)/i.test(lower);
  const inSubprojectRootDoc = /(\/landing\/|\/congreso.api\/).+\.(md|txt|http)$/.test(lower) && /(audit|auditoria|analisis|resultado|performance|qa|reporte|informe)/i.test(lower);

  return inDocs || inLandingDocs || isRootDoc || inSubprojectRootDoc;
}

function contentType(rel) {
  const lower = rel.toLowerCase();
  if (/(audit|auditoria)/.test(lower)) return 'auditoría';
  if (/(analisis|analysis)/.test(lower)) return 'análisis';
  if (/(resultado|resultados)/.test(lower)) return 'resultado';
  if (/(performance)/.test(lower)) return 'performance';
  if (/(qa)/.test(lower)) return 'qa';
  if (/(reporte|report|informe)/.test(lower)) return 'informe';
  if (/(pendientes)/.test(lower)) return 'pendientes';
  if (/(plan)/.test(lower)) return 'plan';
  if (/(inventario)/.test(lower)) return 'inventario';
  if (/(alineacion|alineación|matriz|validacion|validación)/.test(lower)) return 'documentación técnica';
  return 'documentación';
}

function reasonFor(rel) {
  const type = contentType(rel);
  const location = rel.toLowerCase().includes('/docs/') || rel.toLowerCase().startsWith('docs/')
    ? 'Ubicado en carpeta de documentación'
    : 'Documento en la raíz/subproyecto';
  return `Texto plano de ${type}; ${location}, sin impacto en runtime/backend/frontend. Movido para revisión previa a eliminación.`;
}

async function ensureDir(p) {
  await fs.promises.mkdir(p, { recursive: true });
}

async function moveFilePreserveStructure(rel) {
  const src = path.join(root, rel);
  const dest = path.join(trashDir, rel);
  await ensureDir(path.dirname(dest));
  // Si existe, agregar sufijo de tiempo para evitar colisiones
  const exists = await statSafe(dest);
  if (exists && exists.isFile()) {
    const { name, ext } = path.parse(dest);
    const alt = path.join(path.dirname(dest), `${name}.${Date.now()}${ext}`);
    await fs.promises.rename(src, alt);
    return path.relative(root, alt);
  } else {
    await fs.promises.rename(src, dest);
    return path.relative(root, dest);
  }
}

async function main() {
  const candidates = [];
  for await (const { rel, entry } of walk(root)) {
    if (!entry.isFile()) continue;
    if (isTextCandidate(rel)) {
      candidates.push(rel);
    }
  }

  if (candidates.length === 0) {
    console.log('No se encontraron candidatos para mover a trash.');
    return;
  }

  await ensureDir(trashDir);

  const moved = [];
  for (const rel of candidates) {
    try {
      const newRel = await moveFilePreserveStructure(rel);
      moved.push({
        name: path.basename(rel),
        original: rel,
        newLocation: newRel,
        type: contentType(rel),
        reason: reasonFor(rel),
      });
      console.log(`Movido: ${rel} -> ${newRel}`);
    } catch (err) {
      console.error(`Error moviendo ${rel}:`, err.message);
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

main().catch(err => {
  console.error('Error ejecutando limpieza a trash:', err);
  process.exit(1);
});