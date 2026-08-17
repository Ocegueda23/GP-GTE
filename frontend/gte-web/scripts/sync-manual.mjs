import { copyFileSync, existsSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const carpetaActual = dirname(fileURLToPath(import.meta.url));
const origen = resolve(carpetaActual, "..", "..", "..", "Doctos", "ManualUsuarioGTE.html");
const destino = resolve(carpetaActual, "..", "public", "manual-usuario.html");

if (!existsSync(origen)) {
  console.error(`No se encontro el manual de usuario en ${origen}`);
  process.exit(1);
}

mkdirSync(dirname(destino), { recursive: true });
copyFileSync(origen, destino);
console.log(`Manual de usuario sincronizado: ${origen} -> ${destino}`);
