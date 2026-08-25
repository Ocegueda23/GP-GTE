USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      44_2026-08-24_INSERT_bdsGTE_ReglasNegocioGTE.sql
   Autor:       Equipo GTE
   Descripcion: Siembra el proyecto GTE y sus 40 reglas de negocio en el
                catalogo creado por el script 43.

                GTE se da de alta como un proyecto mas (Clave 'GTE'):
                el modelo del catalogo exige que toda regla tenga un
                proyecto dueno, y GTE es un sistema como cualquier otro
                de los que mantiene el departamento. No se introduce
                ningun caso especial.

                Los 9 ambitos son FLUJOS DE OPERACION (tipo 1) y salen
                de los modulos de la seccion 3 del Documento Maestro.

                ORIGEN DE LOS DATOS: seccion 3 de
                Doctos/GTE-DocumentoMaestro.md. Los enunciados son
                transcripcion condensada del documento, no redaccion
                nueva.

                ESTADOS SEMBRADOS Y POR QUE:
                - Vigente (25): la clave RN-* aparece referenciada en el
                  codigo de src/, o el codigo la implementa de forma
                  explicita bajo otro comentario (caso de RN-QA-05, que
                  vive dentro de ValidarRevisionPruebasAsync bajo el
                  encabezado de RN-QA-04).
                - Implementada parcialmente (1): RN-PRY-02. Doctos/
                  PENDIENTES.md documenta implementadas las reglas de
                  proyectos EsMantenimiento/Administrado, pero la clave
                  no esta etiquetada en el codigo y no se pudo confirmar
                  el segundo supuesto de la regla (mover un WorkItem
                  fuera del proyecto).
                - Documentada sin implementar (14): la clave no aparece
                  en el codigo y no se encontro evidencia de que la
                  regla corra. REQUIEREN REVISION DEL EQUIPO: algunas
                  pueden estar implementadas sin la etiqueta.

                DOS DISCREPANCIAS DETECTADAS AL SEMBRAR (documentadas
                aqui a proposito, es justo lo que el catalogo existe
                para hacer visible):

                1. RN-PLA-05 existe en el CODIGO
                   (PlaneacionQueryService, marcada "nueva") y NO existe
                   en el Documento Maestro. Se siembra con el enunciado
                   tomado del comentario del codigo. El Documento
                   Maestro deberia actualizarse.

                2. RN-QA-06 significa cosas DISTINTAS en cada fuente:
                   - Documento Maestro: no se puede rechazar sin un
                     hallazgo pendiente registrado.
                   - Codigo (CambiarEstatusWorkItemCommand): en
                     proyectos categoria Desarrollo, terminar directo
                     desde En Proceso saltando En Pruebas exige el
                     permiso WI.SaltarPruebas.
                   Se siembra la version del CODIGO, porque es la que
                   realmente corre. La regla del documento (no rechazar
                   sin hallazgo) SI esta implementada, pero dentro de
                   RN-QA-04, y asi se siembra. El equipo debe decidir si
                   renumera o corrige el Documento Maestro.

   Requiere:    43 aplicado, y los catalogos 01/03 (tblCategoriaProyecto,
                tblEstatusProyecto, tblProyecto).
   ===================================================================== */
BEGIN TRY

    /* ---------------------------------------------------------------
       1. Proyecto GTE (idempotente por Clave; si ya existe no se pisa).
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM dbo.tblProyecto WHERE Clave = N'GTE')
    BEGIN
        INSERT INTO dbo.tblProyecto
            (Clave, Nombre, IdCategoriaProyecto, IdEstatusProyecto,
             EsMantenimiento, Administrado, UsuarioRegistro, Activo)
        VALUES
            (N'GTE', N'GTE - Gestor Tecnologico Empresarial', 1, 3,
             0, 0, N'script-despliegue', 1)
        PRINT 'OK: proyecto GTE dado de alta'
    END
    ELSE
        PRINT 'SKIP: proyecto GTE ya existe'

    DECLARE @IdProyectoGTE INT = (SELECT IdProyecto FROM dbo.tblProyecto WHERE Clave = N'GTE')

    /* ---------------------------------------------------------------
       2. Ambitos: flujos de operacion de GTE (tipo 1).
       --------------------------------------------------------------- */
    INSERT INTO dbo.tblAmbitoRegla (IdProyecto, IdTipoAmbitoRegla, Nombre, UsuarioRegistro, Activo)
    SELECT @IdProyectoGTE, 1, v.Nombre, N'script-despliegue', 1
    FROM (VALUES
        (N'Administracion'), (N'Portafolio'), (N'Requerimientos'), (N'Planeacion'),
        (N'Desarrollo'), (N'Calidad (QA)'), (N'Releases'), (N'Operacion'), (N'Soporte')
        ) v(Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblAmbitoRegla a
                      WHERE a.IdProyecto = @IdProyectoGTE
                        AND a.IdTipoAmbitoRegla = 1
                        AND a.Nombre = v.Nombre)
    PRINT 'OK: ambitos de GTE sembrados (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    /* ---------------------------------------------------------------
       3. Las 40 reglas.
       --------------------------------------------------------------- */
    DECLARE @Reglas TABLE
    (
        Clave           NVARCHAR(30),
        Ambito          NVARCHAR(200),
        Nombre          NVARCHAR(300),
        Enunciado       NVARCHAR(MAX),
        Estado          INT,
        PermisoBypass   NVARCHAR(100),
        UbicacionCodigo NVARCHAR(400)
    )

    INSERT INTO @Reglas (Clave, Ambito, Nombre, Enunciado, Estado, PermisoBypass, UbicacionCodigo)
    VALUES
    -- Administracion
    (N'RN-ADM-01', N'Administracion', N'Sin ciclos en la jerarquia de jefes',
     N'Un usuario no puede ser su propio jefe ni formar ciclos en la jerarquia. Se valida con CTE recursivo antes de guardar.',
     1, NULL, N'src/GTE.Domain/Interfaces/IAdministracionRepository.cs'),
    (N'RN-ADM-02', N'Administracion', N'Administrador no cortocircuita las reglas de negocio',
     N'El rol Administrador no cortocircuita las validaciones de negocio (a diferencia del EsAdmin del GT): solo otorga todos los permisos, y las reglas duras aplican a todos. Excepcion acotada (equipo, 2026-08-02): el cierre de WorkItems (RN-REQ-03) y el ownership de cambios de estatus (RN-REQ-05) si se saltan para quien tenga el permiso WI.OmitirValidacionCierre, sembrado solo para Administrador -- via RBAC por datos, sin cortocircuito de codigo por rol.',
     1, N'WI.OmitirValidacionCierre', N'src/GTE.Application/Interfaces/IVerificadorPermisos.cs'),
    (N'RN-ADM-03', N'Administracion', N'La jerarquia define la visibilidad por defecto',
     N'La jerarquia jefe-subordinado define el alcance de visibilidad por defecto de bandejas y reportes (CTE recursivo, heredado del GT); los permisos pueden ampliarlo.',
     3, NULL, NULL),
    (N'RN-ADM-04', N'Administracion', N'Cambiar de nivel no recalcula presupuestos ya asignados',
     N'Los cambios de nivel de un usuario NO recalculan presupuestos de WorkItems ya asignados: el presupuesto se fija al asignar y queda en el historial de campo.',
     3, NULL, NULL),
    -- Portafolio
    (N'RN-PRY-01', N'Portafolio', N'No se cierra un proyecto con WorkItems abiertos',
     N'Un proyecto con WorkItems abiertos no puede cerrarse: responde 409 con la lista de pendientes (patron de conflicto estructurado).',
     1, NULL, N'src/GTE.Domain/Interfaces/IAdministracionRepository.cs'),
    (N'RN-PRY-02', N'Portafolio', N'Reglas especiales de proyectos de mantenimiento',
     N'Los proyectos con EsMantenimiento = 1 conservan las reglas especiales del GT: cerrar un WorkItem exige el permiso WI.TerminarMantenimiento, y mover un WorkItem fuera del proyecto exige permiso de administrador del proyecto.',
     2, N'WI.TerminarMantenimiento', NULL),
    (N'RN-PRY-03', N'Portafolio', N'Riesgo con exposicion alta notifica al responsable',
     N'La exposicion de riesgo mayor o igual a 15 (de 25) notifica automaticamente al responsable del proyecto y aparece en el dashboard ejecutivo.',
     3, NULL, NULL),
    -- Requerimientos
    (N'RN-REQ-01', N'Requerimientos', N'Una sola tarea En Proceso por persona',
     N'Al ejecutar INICIAR sobre un WorkItem, si el asignado tiene otro item En Proceso, ese otro item se suspende automaticamente y el historial se registra EN EL ITEM SUSPENDIDO (corrige el bug 4 del GT, que escribia el historial en la tarea equivocada). La regla es configurable por tipo de item.',
     1, NULL, N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    (N'RN-REQ-02', N'Requerimientos', N'INICIAR exige fecha compromiso',
     N'La accion INICIAR exige que FechaCompromiso este capturada.',
     1, NULL, N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    (N'RN-REQ-03', N'Requerimientos', N'TERMINAR exige avance registrado y cero hallazgos pendientes',
     N'TERMINAR exige al menos un registro de tiempo o una subtarea hija terminada, y cero revisiones con Corregido = 0. Una sola implementacion en el dominio (corrige los dos caminos inconsistentes de FrmRegistro y FrmTareaSTS.btnTerminar del GT).',
     1, N'WI.OmitirValidacionCierre', N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    (N'RN-REQ-04', N'Requerimientos', N'Fecha compromiso no puede quedar en el pasado',
     N'FechaCompromiso no puede ser anterior a hoy salvo con el permiso WI.ModificarCompromiso. Ampliacion 2026-08-04: cualquier cambio a una fecha compromiso YA capturada exige ese permiso; fijarla la primera vez sigue libre.',
     1, N'WI.ModificarCompromiso', N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    (N'RN-REQ-05', N'Requerimientos', N'Ownership: no se toca el item ajeno ni el terminado',
     N'Editar un item Terminado exige WI.ModificarTerminado. Editar, cambiar el estatus, registrar tiempo o marcar CORREGIDO un hallazgo en un item ajeno exige WI.ModificarAjeno, con el mismo gate en ActualizarWorkItemCommand, CambiarEstatusWorkItemCommand, RegistrarTiempoCommand y CorregirRevisionCommand. "Ajeno" INCLUYE sin asignar (decision del equipo 2026-08-02): nadie toma trabajo del backlog solo con INICIAR o registrando tiempo. Quedan deliberadamente FUERA del gate comentar, adjuntar archivos y reportar un hallazgo: son acciones de colaboracion hechas por definicion por alguien mas.',
     1, N'WI.ModificarAjeno', N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    (N'RN-REQ-06', N'Requerimientos', N'Eliminar solo en Borrador o Pendiente',
     N'Eliminar solo procede en estatus Borrador o Pendiente y con el permiso WI.Eliminar: hard delete si es Borrador, baja logica si es Pendiente.',
     3, N'WI.Eliminar', NULL),
    (N'RN-REQ-07', N'Requerimientos', N'COPIAR limpia estatus, compromiso e historial',
     N'COPIAR duplica el item limpiando estatus (lo fija el backend), compromiso e historial; la complejidad queda siempre en la minima y el titulo recibe el sufijo " - Copia" (regla vigente del GT).',
     3, NULL, NULL),
    (N'RN-REQ-08', N'Requerimientos', N'El presupuesto se calcula al asignar y se congela',
     N'Al asignar un item, MinutosPresupuesto se calcula de tblMatrizPresupuesto (IdComplejidad por nivel del asignado) y se congela.',
     1, NULL, N'src/GTE.Domain/Interfaces/IWorkItemRepository.cs'),
    (N'RN-REQ-09', N'Requerimientos', N'Items sin movimiento alertan al lider',
     N'Los estatus con mas de N dias sin movimiento generan alerta al lider. N es parametro por proyecto.',
     3, NULL, NULL),
    -- Planeacion
    (N'RN-PLA-01', N'Planeacion', N'Sobrecargar un sprint requiere confirmacion',
     N'Asignar mas puntos que la velocidad historica mas 20 por ciento al planear un sprint requiere confirmacion explicita. Es advertencia suave, no bloqueo.',
     3, NULL, NULL),
    (N'RN-PLA-02', N'Planeacion', N'Cerrar sprint reubica los items no terminados',
     N'Cerrar un sprint mueve automaticamente los items no terminados al backlog o al siguiente sprint (lo decide el usuario en el cierre) y registra el movimiento.',
     1, NULL, N'src/GTE.Domain/Planeacion/EstatusSprint.cs'),
    (N'RN-PLA-03', N'Planeacion', N'Un WorkItem en un solo sprint a la vez',
     N'Un WorkItem solo puede estar en un sprint a la vez.',
     1, NULL, NULL),
    (N'RN-PLA-04', N'Planeacion', N'El limite WIP bloquea el drop',
     N'Exceder el limite WIP de una columna bloquea el drop con explicacion, o permite override con el permiso PLA.SaltarWip, registrado en bitacora.',
     1, N'PLA.SaltarWip', N'src/GTE.Domain/Interfaces/IPlaneacionRepository.cs'),
    (N'RN-PLA-05', N'Planeacion', N'La columna Terminado solo muestra el mes en curso',
     N'En el tablero, la columna Terminado solo incluye los items cerrados en el mes en curso, para que no acumule meses de historial. ATENCION: esta regla existe en el codigo pero NO esta en el Documento Maestro; hay que documentarla ahi.',
     1, NULL, N'src/GTE.Infrastructure/Services/PlaneacionQueryService.cs'),
    -- Desarrollo
    (N'RN-DEV-01', N'Desarrollo', N'Webhooks autenticados por secreto compartido',
     N'Los webhooks se autentican por secreto compartido por repositorio; los payloads no autenticados se descartan y se registran.',
     3, NULL, NULL),
    (N'RN-DEV-02', N'Desarrollo', N'Commit y WorkItem se vinculan por folio',
     N'La vinculacion commit-WorkItem es por folio en el mensaje del commit; un commit puede vincular varios items.',
     3, NULL, NULL),
    (N'RN-DEV-03', N'Desarrollo', N'Las transiciones por eventos Git pasan por el motor',
     N'Las transiciones automaticas por eventos Git son configurables por proyecto y siempre pasan por el motor de workflow, nunca por UPDATE directo del estatus.',
     3, NULL, NULL),
    -- Calidad (QA)
    (N'RN-QA-01', N'Calidad (QA)', N'No se aprueba un release con calidad pendiente',
     N'Un release no puede aprobarse con casos en Falla sin bug asociado, ni con bugs S1/S2 abiertos. Responde 409 estructurado con la lista.',
     1, NULL, N'src/GTE.Domain/Interfaces/IEntregaRepository.cs'),
    (N'RN-QA-02', N'Calidad (QA)', N'Reabrir un hallazgo corregido exige rol Lider',
     N'Reabrir un hallazgo ya corregido exige rol Lider (permiso REV.Reabrir). Regla vigente heredada del GT.',
     1, N'REV.Reabrir', N'src/GTE.Application/Revisiones'),
    (N'RN-QA-03', N'Calidad (QA)', N'Un hallazgo pendiente regresa el item a Correccion',
     N'El estatus del WorkItem reacciona a las revisiones: si alguna queda con Corregido = 0, el item regresa de Terminado a Correccion via workflow (formaliza ValidarSiHayRevisionesPendientes del GT).',
     1, NULL, NULL),
    (N'RN-QA-04', N'Calidad (QA)', N'Aprobar o rechazar pruebas exige permiso, y rechazar exige hallazgo',
     N'Aprobar (TERMINAR) o rechazar (RECHAZAR_QA) la fase de pruebas de un WorkItem desde En Pruebas exige el permiso WI.AprobarPruebas, sembrado para el rol QA por datos en tblTransicionConfig.RequierePermiso (no es cortocircuito de codigo). Ademas, rechazar exige que ya exista un hallazgo pendiente registrado: un motivo de texto libre no basta. El TERMINAR desde En Proceso (ruta de proyectos sin fase QA) no exige el permiso.',
     1, N'WI.OmitirValidacionCierre', N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    (N'RN-QA-05', N'Calidad (QA)', N'Sin autoaprobacion ni autorechazo de pruebas',
     N'Quien aprueba o rechaza la fase de pruebas no puede ser el propio asignado del WorkItem. Por esto mismo, el gate de item ajeno (RN-REQ-05) se excluye a proposito para estas dos transiciones: lo normal es que el revisor sea otra persona.',
     1, N'WI.OmitirValidacionCierre', N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    (N'RN-QA-06', N'Calidad (QA)', N'En proyectos de Desarrollo no se salta la fase de pruebas',
     N'En proyectos de categoria Desarrollo, terminar un WorkItem directo desde En Proceso sin pasar por En Pruebas exige el permiso WI.SaltarPruebas. Las categorias TI y Mantenimiento quedan libres. ATENCION: el Documento Maestro usa la clave RN-QA-06 para OTRA regla (no rechazar sin hallazgo); aqui se siembra la version del codigo, que es la que corre. Esa otra regla esta implementada dentro de RN-QA-04.',
     1, N'WI.SaltarPruebas', N'src/GTE.Application/WorkItems/Commands/CambiarEstatusWorkItemCommand.cs'),
    -- Releases
    (N'RN-REL-01', N'Releases', N'Solo entra al release lo Terminado y revisado',
     N'Solo los WorkItems en estatus Terminado y revisados pueden agregarse a un release.',
     1, NULL, N'src/GTE.Domain/Entregas/ModelosEntregas.cs'),
    (N'RN-REL-02', N'Releases', N'Todo script de despliegue lleva rollback pareado',
     N'Todo script SQL de despliegue debe tener script de rollback asociado, o justificacion explicita de irreversibilidad. El campo es obligatorio.',
     1, NULL, N'src/GTE.Domain/Entregas/ConstantesEntregas.cs'),
    (N'RN-REL-03', N'Releases', N'PROD exige la cadena de aprobaciones completa',
     N'El paso a PROD exige que todas las aprobaciones de la cadena esten en Aprobado.',
     1, NULL, N'src/GTE.Domain/Entregas'),
    (N'RN-REL-04', N'Releases', N'Publicar un release notifica a los solicitantes',
     N'Publicar un release notifica a los solicitantes de los WorkItems incluidos, con el mensaje del tipo "tu peticion SOL-2026-0045 se libero en la version 2.11".',
     3, NULL, NULL),
    -- Operacion
    (N'RN-OPS-01', N'Operacion', N'Incidente S1 notifica de inmediato y escala a los 30 minutos',
     N'Un incidente S1 notifica de inmediato por todos los canales al responsable del sistema y al lider, con escalamiento a los 30 minutos sin atencion.',
     1, NULL, N'src/GTE.Application/Operacion/Commands/CrearIncidenteCommand.cs'),
    (N'RN-OPS-02', N'Operacion', N'Cerrar un incidente S1/S2 exige causa raiz',
     N'Cerrar un incidente S1 o S2 exige causa raiz documentada.',
     1, NULL, N'src/GTE.Application/Operacion/Commands/CambiarEstatusIncidenteCommand.cs'),
    (N'RN-OPS-03', N'Operacion', N'Cambiar severidad exige motivo registrado',
     N'Un incidente puede degradarse o escalarse de severidad solo con motivo registrado, que queda auditado en bitacora.',
     1, NULL, N'src/GTE.Application/Operacion/Commands/CambiarSeveridadIncidenteCommand.cs'),
    -- Soporte
    (N'RN-SUP-01', N'Soporte', N'El reloj de SLA corre solo en horario laboral',
     N'El reloj de SLA corre solo en el horario laboral del equipo asignado y se pausa cuando el ticket esta en Esperando Usuario.',
     3, NULL, NULL),
    (N'RN-SUP-02', N'Soporte', N'Alerta al 80 por ciento del SLA y escalamiento al 100',
     N'Al consumir el 80 por ciento del tiempo de SLA sin resolucion se alerta al agente; al 100 por ciento se escala al lider y se registra el incumplimiento.',
     3, NULL, NULL),
    (N'RN-SUP-03', N'Soporte', N'Ticket Resuelto se cierra solo a los 5 dias habiles',
     N'Un ticket en estatus Resuelto se cierra automaticamente a los 5 dias habiles sin respuesta del usuario.',
     3, NULL, NULL)

    INSERT INTO dbo.tblReglaNegocio
        (IdProyecto, Clave, Nombre, Enunciado, IdAmbitoRegla, IdEstadoReglaNegocio,
         PermisoBypass, UbicacionCodigo, VersionActual, UsuarioRegistro, Activo)
    SELECT @IdProyectoGTE, r.Clave, r.Nombre, r.Enunciado, a.IdAmbitoRegla, r.Estado,
           r.PermisoBypass, r.UbicacionCodigo, 1, N'script-despliegue', 1
    FROM @Reglas r
    INNER JOIN dbo.tblAmbitoRegla a
        ON a.IdProyecto = @IdProyectoGTE AND a.IdTipoAmbitoRegla = 1 AND a.Nombre = r.Ambito
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblReglaNegocio rn
                      WHERE rn.IdProyecto = @IdProyectoGTE AND rn.Clave = r.Clave)
    PRINT 'OK: reglas de GTE sembradas (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    /* ---------------------------------------------------------------
       4. Version 1 de cada regla recien sembrada.
       --------------------------------------------------------------- */
    INSERT INTO dbo.tblReglaNegocioVersion
        (IdReglaNegocio, NumeroVersion, Enunciado, Justificacion, MotivoCambio, UsuarioRegistro)
    SELECT rn.IdReglaNegocio, 1, rn.Enunciado, rn.Justificacion,
           N'Alta inicial desde Doctos/GTE-DocumentoMaestro.md', N'script-despliegue'
    FROM dbo.tblReglaNegocio rn
    WHERE rn.IdProyecto = @IdProyectoGTE
      AND NOT EXISTS (SELECT 1 FROM dbo.tblReglaNegocioVersion v
                      WHERE v.IdReglaNegocio = rn.IdReglaNegocio AND v.NumeroVersion = 1)
    PRINT 'OK: version 1 registrada (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    /* ---------------------------------------------------------------
       5. Relaciones entre reglas conocidas.
       --------------------------------------------------------------- */
    INSERT INTO dbo.tblReglaNegocioRelacion
        (IdReglaNegocio, IdReglaNegocioRelacionada, IdTipoRelacionRegla, Nota, UsuarioRegistro, Activo)
    SELECT a.IdReglaNegocio, b.IdReglaNegocio, v.Tipo, v.Nota, N'script-despliegue', 1
    FROM (VALUES
        (N'RN-REQ-03', N'RN-ADM-02', 2,
         N'El bypass de cierre WI.OmitirValidacionCierre se define en RN-ADM-02'),
        (N'RN-REQ-05', N'RN-ADM-02', 2,
         N'El bypass de ownership se define en RN-ADM-02'),
        (N'RN-QA-05',  N'RN-REQ-05', 3,
         N'RN-QA-05 excluye a proposito el gate de item ajeno de RN-REQ-05'),
        (N'RN-QA-06',  N'RN-REQ-03', 3,
         N'Conflicto de orden real: el gate de RN-QA-06 corre ANTES del de RN-REQ-03 y puede taparlo (403 en vez de 400). Documentado en PENDIENTES.md seccion 5'),
        (N'RN-QA-04',  N'RN-QA-05', 1,
         N'Se validan juntas en ValidarRevisionPruebasAsync')
        ) v(ClaveA, ClaveB, Tipo, Nota)
    INNER JOIN dbo.tblReglaNegocio a ON a.IdProyecto = @IdProyectoGTE AND a.Clave = v.ClaveA
    INNER JOIN dbo.tblReglaNegocio b ON b.IdProyecto = @IdProyectoGTE AND b.Clave = v.ClaveB
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblReglaNegocioRelacion x
                      WHERE x.IdReglaNegocio = a.IdReglaNegocio
                        AND x.IdReglaNegocioRelacionada = b.IdReglaNegocio
                        AND x.IdTipoRelacionRegla = v.Tipo)
    PRINT 'OK: relaciones entre reglas sembradas (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
