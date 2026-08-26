/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** URL base del API. Vacia en produccion: el API sirve la SPA desde su propio origen. */
  readonly VITE_API_URL?: string;
  /**
   * Sello de la publicacion (aaaa.MM.dd.HHmm) que estampa publicar.bat. Ausente en
   * `npm run dev`, donde la barra superior no muestra numero de version.
   */
  readonly VITE_VERSION?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
