import { useEffect, useState } from "react";

/**
 * Indica si Bloq Mayus esta activo, para avisarlo junto a los campos de contraseña: el
 * texto va enmascarado y, sin el aviso, un login fallido por la tecla trabada parece una
 * contraseña equivocada.
 *
 * El estado real solo lo trae `getModifierState` de un evento de teclado o de raton, asi
 * que se escucha a nivel documento (keydown/keyup/click) en vez de en un input concreto:
 * asi el aviso aparece aunque la tecla se hubiera activado antes de enfocar el campo, en
 * cuanto el usuario toca cualquier tecla o hace clic en la pagina.
 */
export function useBloqMayusculas() {
  const [activo, setActivo] = useState(false);

  useEffect(() => {
    const actualizar = (evento: KeyboardEvent | MouseEvent) => {
      // getModifierState no existe en todos los eventos sinteticos (ej. jsdom en pruebas).
      if (typeof evento.getModifierState !== "function") return;
      setActivo(evento.getModifierState("CapsLock"));
    };

    document.addEventListener("keydown", actualizar);
    document.addEventListener("keyup", actualizar);
    document.addEventListener("click", actualizar);
    return () => {
      document.removeEventListener("keydown", actualizar);
      document.removeEventListener("keyup", actualizar);
      document.removeEventListener("click", actualizar);
    };
  }, []);

  return activo;
}
