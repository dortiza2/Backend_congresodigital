#!/usr/bin/env node
/*
  Genera clearproject.md con:
  - Inventario completo (ruta, tipo, tamaño)
  - Clasificación: activo/redundante/crítico
  - Explicaciones críticas y evaluación de impacto
  - Recomendaciones de limpieza
  - Sección especial de pruebas históricas/informativos obsoletos
*/
const fs = require('fs');
const path = require('path');

const root = process.cwd();
const projectName = 'Congreso_digital';
const targetDir = root; // Asumir ejecución desde la raíz del repo

/** Utils **/
async function statSafe(p) {
  try { return await fs.promises.stat(p); } catch { return null; }
}

async function* walk(dir) {
  const entries = await fs.promises.readdir(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    yield { full, entry: e };
    if (e.isDirectory()) {
      // Ignorar bin/obj/node_modules para rendimiento si existieran
      if (/\b(node_modules|bin|obj)\b/.test(full)) continue;
      // Evitar recorrer trash consolidado
      if (/\b(trash)\b/.test(full)) continue;
      yield* walk(full);
    }
  }
}

async function dirSize(dir) {
  let total = 0;
  for await (const { full, entry } of walk(dir)) {
    if (entry.isFile()) {
      const st = await statSafe(full);
      if (st) total += st.size;
    }
  }
  return total;
}

function classify(p, type) {
  const rel = path.relative(targetDir, p);
  const isDir = type === 'dir';
  const lower = rel.toLowerCase();
  const ext = path.extname(rel).toLowerCase();

  // Heurísticas
  // reconocer estructura bajo desarrollo/
  const isApi = lower.startsWith('congreso.api/') || lower.startsWith('desarrollo/congreso.api/');
  const isFrontend = lower.startsWith('landing/') || lower.startsWith('desarrollo/landing/');
  const isDb = lower.startsWith('db/');
  const isDocs = lower.startsWith('docs/');
  const isTrash = lower.startsWith('__trash__/') || lower.startsWith('_trash/');
  const isTrashConsolidated = lower.startsWith('trash/');
  const isSecurity = lower.startsWith('db/security/');
  const isMigrations = lower.startsWith('db/migrations/');
  const isScripts = lower.startsWith('scripts/');
  const isTools = lower.startsWith('tools/');
  const isTests = lower.startsWith('tests/') || lower.includes('/tests/');
  const isAudit = /audit|auditoria|analisis|resultado|performance|qa|reporte|informe/.test(lower);
  const isEnvGuide = lower.endsWith('env_variables_guide.md');
  const isVscode = lower.startsWith('.vscode/');
  const isRootPkg = rel === 'package.json' || rel === 'package-lock.json';
  const isImagesUnused = lower.startsWith('imagenesausar/');

  // Clasificación principal
  if (isSecurity || isEnvGuide) return 'crítico';
  if (isMigrations) return 'crítico';
  if (isApi || isFrontend || isDb || isScripts) return 'activo';
  if (isRootPkg) return 'activo';
  if (isVscode) return 'crítico'; // configuración de desarrollo/depuración
  if (isTrash || isTrashConsolidated || isImagesUnused) return 'redundante';
  if (isTools) return 'activo';
  if (isDocs && (isAudit || isTests)) return 'redundante';
  if (isTests) return 'activo'; // pruebas funcionales del proyecto
  // Por defecto: activo si está dentro del repo y no es explícitamente basura
  return isDir ? 'activo' : 'activo';
}

function impactAssessment(rel, classification) {
  // Evaluación de impacto por eliminación
  if (classification === 'crítico') {
    if (rel.startsWith('db/security/')) return 'Alto: puede eliminar guías y scripts de hardening; riesgo en seguridad.';
    if (rel.startsWith('db/migrations/')) return 'Alto: sin migraciones el esquema no puede desplegarse ni versionarse.';
    if (rel.startsWith('.vscode/')) return 'Medio: afecta debuggers y tareas de desarrollo locales.';
    if (rel.endsWith('ENV_VARIABLES_GUIDE.md')) return 'Medio: pérdida de guía de configuración de secretos/entorno.';
    return 'Alto: dependencias de configuración o seguridad potencialmente afectadas.';
  }
  if (classification === 'activo') {
    if (rel.startsWith('Congreso.Api/') || rel.startsWith('landing/')) return 'Alto: rompe la aplicación backend/frontend.';
    if (rel.startsWith('db/')) return 'Medio: afecta scripts y soporte de BD.';
    if (rel.startsWith('scripts/')) return 'Bajo-Medio: pierde automatización útil.';
    if (rel === 'package.json' || rel === 'package-lock.json') return 'Medio: afecta gestión de dependencias Node en el repo.';
    return 'Bajo: posible impacto limitado, revisar dependencia antes de eliminar.';
  }
  if (classification === 'redundante') {
    if (rel.startsWith('__trash__/') || rel.startsWith('_trash/')) return 'Bajo: contenedor de basura, seguro de eliminar tras validar contenido.';
    if (rel.startsWith('imagenesausar/')) return 'Bajo: recursos no referenciados, limpieza segura tras verificación manual.';
    if (rel.startsWith('docs/')) return 'Bajo: documentación histórica, impacto nulo en runtime.';
    return 'Bajo: sin referencias actuales aparentes.';
  }
  return 'Desconocido: requiere validación manual.';
}

function criticalExplanation(rel) {
  if (rel.startsWith('db/security/lockdown.sql')) return 'Script de endurecimiento/validación de privilegios y objetos en PostgreSQL; útil para auditorías y seguridad operativa.';
  if (rel.startsWith('db/migrations/')) return 'Conjunto de migraciones/versionado del esquema de base de datos; imprescindible para despliegues consistentes.';
  if (rel.endsWith('ENV_VARIABLES_GUIDE.md')) return 'Documento guía para variables de entorno y secretos; evita errores de configuración y exposición de credenciales.';
  if (rel.startsWith('.vscode/')) return 'Configuración de depuración y tareas automatizadas para el entorno de desarrollo; su eliminación dificulta flujos dev.';
  return 'Elemento con dependencia oculta o relevancia en seguridad/configuración; revisar antes de eliminar.';
}

async function main() {
  const items = [];
  for await (const { full, entry } of walk(targetDir)) {
    const rel = path.relative(targetDir, full) || '.';
    const type = entry.isDirectory() ? 'dir' : entry.isFile() ? 'file' : 'other';
    let size = 0;
    if (type === 'file') {
      const st = await statSafe(full);
      size = st ? st.size : 0;
    } else if (type === 'dir') {
      size = await dirSize(full);
    }
    const classification = classify(full, type);
    const impact = impactAssessment(rel, classification);
    const criticalInfo = classification === 'crítico' ? criticalExplanation(rel) : '';
    items.push({ rel, full, type, size, classification, impact, criticalInfo });
  }

  // Sección especial: archivos de pruebas históricas e informativos obsoletos
  const special = items.filter(it => /(^docs\/|\/docs\/).*/.test(it.rel) && /audit|auditoria|analisis|resultado|performance|qa|reporte|informe/i.test(it.rel));

  // Recomendaciones priorizadas de limpieza: redundantes ordenados por tamaño desc
  const redundants = items.filter(it => it.classification === 'redundante').sort((a,b) => b.size - a.size);

  // Construir Markdown
  let md = '';
  md += `# Informe de Limpieza Técnica — clearproject.md\n\n`;
  md += `Proyecto: ${projectName}\nRuta base: ${targetDir}\nFecha: ${new Date().toISOString()}\n\n`;

  md += `## Inventario Completo\n`;
  md += `| Ruta | Tipo | Tamaño (bytes) | Clasificación |\n| --- | --- | ---: | --- |\n`;
  for (const it of items) {
    md += `| ${it.full} | ${it.type} | ${it.size} | ${it.classification} |\n`;
  }

  md += `\n## Clasificación y Justificación\n`;
  md += `| Ruta | Clasificación | Impacto por eliminación |\n| --- | --- | --- |\n`;
  for (const it of items) {
    md += `| ${it.full} | ${it.classification} | ${it.impact} |\n`;
  }

  md += `\n## Elementos Críticos — Explicación Técnica\n`;
  md += `| Ruta | Explicación |\n| --- | --- |\n`;
  for (const it of items.filter(i => i.classification === 'crítico')) {
    md += `| ${it.full} | ${it.criticalInfo} |\n`;
  }

  md += `\n## Evaluación de Impacto — Eliminaciones Potenciales\n`;
  md += `- Activos: evitar eliminar; alto riesgo operacional.\n`;
  md += `- Críticos: no eliminar; riesgo alto en seguridad/configuración.\n`;
  md += `- Redundantes: candidatos a limpieza; riesgo bajo, validar contenido.\n`;

  md += `\n## Recomendaciones Priorizadas de Limpieza\n`;
  md += `| Ruta | Tamaño | Riesgo | Recomendación |\n| --- | ---: | --- | --- |\n`;
  for (const r of redundants) {
    const riesgo = 'bajo';
    const recomendacion = r.rel.startsWith('__trash__/') || r.rel.startsWith('_trash/')
      ? 'Eliminar carpeta de basura tras verificación rápida'
      : r.rel.startsWith('docs/')
      ? 'Archivar fuera del repo o eliminar'
      : r.rel.startsWith('imagenesausar/')
      ? 'Eliminar si no referenciado por frontend'
      : 'Eliminar/archivar tras confirmar no uso';
    md += `| ${r.full} | ${r.size} | ${riesgo} | ${recomendacion} |\n`;
  }

  md += `\n## Histórico de Pruebas e Informativos Obsoletos\n`;
  md += `| Ruta | Tamaño | Nota |\n| --- | ---: | --- |\n`;
  for (const s of special) {
    md += `| ${s.full} | ${s.size} | Documento histórico/auditoría; no afecta runtime |\n`;
  }

  md += `\n---\n\nNota: Este informe es analítico y no ejecuta modificaciones automáticas. Todas las acciones de limpieza deben ser aprobadas manualmente.`;

  await fs.promises.writeFile(path.join(targetDir, 'clearproject.md'), md, 'utf8');
  console.log(`Informe generado en ${path.join(targetDir, 'clearproject.md')}`);
}

main().catch(err => {
  console.error('Error generando informe:', err);
  process.exit(1);
});