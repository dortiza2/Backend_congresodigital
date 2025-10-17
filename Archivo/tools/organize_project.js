#!/usr/bin/env node
/**
 * Organiza el proyecto:
 * - Consolida todas las carpetas tipo trash (__trash__, _trash, trash/) en una sola `trash/` bajo la raíz.
 * - Preserva carpetas críticas: `node_modules`, `.git`, `.vscode`, `Congreso.Api`, `landing`.
 * - Opcionalmente mueve el contenido activo bajo `desarrollo/` (sin tocar rutas críticas) cuando se use `--move-to-desarrollo`.
 * - Soporta modo dry-run con `--dry-run`.
 *
 * Uso:
 *   node tools/organize_project.js --dry-run
 *   node tools/organize_project.js --consolidate-trash
 *   node tools/organize_project.js --move-to-desarrollo
 *   node tools/organize_project.js --consolidate-trash --move-to-desarrollo
 *   node tools/organize_project.js --consolidate-trash --move-to-desarrollo --dry-run
 */

const fs = require('fs');
const path = require('path');

const root = process.cwd();

const args = new Set(process.argv.slice(2));
const DRY_RUN = args.has('--dry-run');
const DO_CONSOLIDATE_TRASH = args.has('--consolidate-trash');
const DO_MOVE_TO_DESARROLLO = args.has('--move-to-desarrollo');

const TRASH_NAMES = new Set(['trash', '_trash', '__trash__']);
const PRESERVE_ROOTS = new Set([
  'node_modules', '.git', '.vscode', 'desarrollo'
]);

function log(action, src, dest = '') {
  const tag = DRY_RUN ? '[DRY]' : '[DO ]';
  console.log(`${tag} ${action}: ${src}${dest ? ' -> ' + dest : ''}`);
}

async function ensureDir(p) {
  await fs.promises.mkdir(p, { recursive: true });
}

async function pathExists(p) {
  try { await fs.promises.access(p); return true; } catch { return false; }
}

async function moveDir(src, dest) {
  await ensureDir(path.dirname(dest));
  if (!DRY_RUN) {
    await fs.promises.rename(src, dest);
  }
}

async function* walkDirs(dir) {
  const entries = await fs.promises.readdir(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      yield full;
      // evitar recursión en node_modules y similares por rendimiento
      if (!TRASH_NAMES.has(e.name) && !['node_modules', '.git'].includes(e.name)) {
        yield* walkDirs(full);
      }
    }
  }
}

async function consolidateTrash() {
  const targetTrash = path.join(root, 'trash');
  await ensureDir(targetTrash);

  const candidates = [];
  for await (const d of walkDirs(root)) {
    const base = path.basename(d);
    if (TRASH_NAMES.has(base) && d !== targetTrash) {
      candidates.push(d);
    }
  }

  for (const src of candidates) {
    const rel = path.relative(root, src);
    const base = path.basename(src); // conserva nombre original como subcarpeta
    const dest = path.join(targetTrash, base);
    log('Consolidate trash', src, dest);
    await ensureDir(path.dirname(dest));
    if (!DRY_RUN) {
      // mover el contenido, no solo renombrar la carpeta para evitar colisiones
      const entries = await fs.promises.readdir(src, { withFileTypes: true });
      await ensureDir(dest);
      for (const e of entries) {
        const s = path.join(src, e.name);
        const d = path.join(dest, e.name);
        if (e.isDirectory()) {
          await fs.promises.rename(s, d);
        } else {
          await fs.promises.rename(s, d);
        }
      }
      // eliminar carpeta original si queda vacía
      await fs.promises.rmdir(src).catch(() => {});
    }
  }
}

function isRootPreserve(name) {
  return PRESERVE_ROOTS.has(name) || TRASH_NAMES.has(name);
}

async function moveActiveToDesarrollo() {
  const desarrollo = path.join(root, 'desarrollo');
  await ensureDir(desarrollo);

  const entries = await fs.promises.readdir(root, { withFileTypes: true });
  for (const e of entries) {
    const name = e.name;
    const src = path.join(root, name);
    if (isRootPreserve(name)) continue; // no mover
    if (!e.isDirectory()) continue; // sólo carpetas top-level

    const dest = path.join(desarrollo, name);
    log('Move to desarrollo', src, dest);
    if (!DRY_RUN) {
      await moveDir(src, dest);
    }
  }
}

async function main() {
  if (!DO_CONSOLIDATE_TRASH && !DO_MOVE_TO_DESARROLLO) {
    console.log('Nada que hacer. Usa --consolidate-trash y/o --move-to-desarrollo. Añade --dry-run para simular.');
    return;
  }

  if (DO_CONSOLIDATE_TRASH) {
    await consolidateTrash();
  }

  if (DO_MOVE_TO_DESARROLLO) {
    await moveActiveToDesarrollo();
  }

  console.log(DRY_RUN ? 'Simulación finalizada.' : 'Organización completada.');
}

main().catch(err => {
  console.error('Error organizando proyecto:', err);
  process.exit(1);
});