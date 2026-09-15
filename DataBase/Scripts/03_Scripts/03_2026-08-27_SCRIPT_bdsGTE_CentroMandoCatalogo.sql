USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      03_2026-08-27_SCRIPT_bdsGTE_CentroMandoCatalogo.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Siembra el catalogo de indicadores del Centro de Mando TI
                (dbo.tblIndicadorGestion) con el modelo del documento de
                Ayuda "Centro de Mando TI":

                  Ambito Comun            16 indicadores, pesos suman 100
                  Ambito Desarrollo       11 indicadores, pesos suman 100
                  Ambito Infraestructura  12 ponderados (suman 100) + 1
                                          solo diagnostico (Peso 0)
                  Ambito Soporte          10 indicadores, pesos suman 100

                El score de un responsable es 60% del bloque Comun y 40%
                del bloque tecnico de su area (ver CalculadoraCentroMando
                en GTE.Domain).

                ORIGEN (Automatico / Manual) ES DELIBERADO Y HONESTO: un
                indicador queda Manual cuando GTE hoy NO tiene la fuente
                para calcularlo (backups, cobertura de monitoreo, deuda
                tecnica, capacitacion...). Esos se muestran como "sin
                datos" y no participan en el score, en vez de inventar un
                numero -- mismo criterio que ya usa el Dashboard P18 con
                DORA "Lead Time for Changes".

                Es idempotente por Clave: re-ejecutarlo NO pisa metas,
                umbrales ni pesos que el equipo haya ajustado despues
                desde la pantalla de administracion (GES.Administrar).
   Requiere:    Script 02 de esta tanda (tblIndicadorGestion).
   ===================================================================== */
BEGIN TRY

    DECLARE @Catalogo TABLE
    (
        Clave              NVARCHAR(100),
        Nombre             NVARCHAR(200),
        Descripcion        NVARCHAR(1000),
        Categoria          NVARCHAR(60),
        Ambito             NVARCHAR(30),
        Origen             NVARCHAR(20),
        Formula            NVARCHAR(500),
        Unidad             NVARCHAR(20),
        Meta               DECIMAL(18,4),
        UmbralAlerta       DECIMAL(18,4),
        Direccion          NVARCHAR(10),
        Peso               DECIMAL(9,4),
        PonderaEnScore     BIT,
        Periodicidad       NVARCHAR(20),
        InterpretacionMala NVARCHAR(500),
        AccionSugerida     NVARCHAR(1000)
    )

    /* ---------------- Ambito COMUN (16, pesos = 100) ---------------- */
    INSERT INTO @Catalogo VALUES
    (N'com.objetivos', N'Cumplimiento de objetivos',
     N'Porcentaje de objetivos y resultados clave del periodo cerrados en tiempo y alcance.',
     N'Cumplimiento', N'Comun', N'Automatico',
     N'ObjetivosCumplidos / ObjetivosComprometidos * 100', N'Porcentaje', 90, 75, N'Subir', 12, 1, N'Trimestral',
     N'Sobre-promete o quedo bloqueado por algo fuera de su control.',
     N'Revisa si la meta era realista y que la bloqueo antes de calificar a la persona.'),

    -- OJO -- ESTE ES EL UNICO INDICADOR DEL CATALOGO QUE NO SIRVE HASTA CALIBRARLO:
    -- "puntos por hora de capacidad" depende por completo de como estime puntos cada equipo
    -- (un punto puede valer media jornada en uno y dos dias en otro), asi que no existe una
    -- meta universal. Los valores de abajo son un ARRANQUE conservador (0.10 = un punto por
    -- cada 10 horas de capacidad), no una meta con respaldo. Calibrarlo con dos o tres meses
    -- de datos reales desde Administracion > Indicadores de gestion ANTES de tomar cualquier
    -- decision con el; mientras tanto pesa poco (8) a proposito.
    (N'com.productividad', N'Productividad ajustada',
     N'Volumen cerrado ponderado por complejidad contra la capacidad disponible del equipo, no conteo simple de tareas. REQUIERE CALIBRACION: la meta depende de como estime puntos cada equipo.',
     N'Productividad', N'Comun', N'Automatico',
     N'PuntosHistoria cerrados / horas de capacidad del equipo', N'Indice', 0.10, 0.05, N'Subir', 8, 1, N'Mensual',
     N'Muy alto tambien es senal: puede ser recorte de calidad.',
     N'Antes de leerlo, confirma que la meta ya se calibro con datos reales del equipo.'),

    (N'com.retrabajo', N'Calidad / retrabajo',
     N'Porcentaje del trabajo cerrado que hubo que rehacer (elementos reabiertos despues de terminados).',
     N'Calidad', N'Comun', N'Automatico',
     N'ItemsReabiertos / ItemsCerrados * 100', N'Porcentaje', 8, 15, N'Bajar', 12, 1, N'Mensual',
     N'Alto suele ser requerimiento mal tomado, no mala ejecucion.',
     N'Verifica si el reproceso viene de la captura del requerimiento (proceso) o de la ejecucion (persona).'),

    (N'com.tiempos', N'Cumplimiento de tiempos',
     N'Porcentaje de compromisos resueltos dentro del plazo pactado.',
     N'Puntualidad', N'Comun', N'Automatico',
     N'ItemsATiempo / ItemsConCompromiso * 100', N'Porcentaje', 90, 75, N'Subir', 14, 1, N'Mensual',
     N'Bajo con carga normal apunta a ejecucion; bajo con carga alta apunta a capacidad.',
     N'Cruza contra el Indice de carga antes de actuar.'),

    (N'com.atencion', N'Atencion de solicitudes',
     N'Relacion entre lo que entra al area y lo que alcanza a atenderse en el periodo.',
     N'Atencion', N'Comun', N'Automatico',
     N'Atendidas / Recibidas', N'Indice', 0.95, 0.8, N'Subir', 6, 1, N'Mensual',
     N'Menor a 1 sostenido: la demanda supera la capacidad instalada.',
     N'Revisa el dimensionamiento del equipo, no solo el desempeno individual.'),

    (N'com.incidentes', N'Incidentes atribuibles',
     N'Cantidad de incidentes registrados en los proyectos del equipo durante el periodo.',
     N'Incidencias', N'Comun', N'Automatico',
     N'Conteo de incidentes del periodo', N'Conteo', 0, 3, N'Bajar', 8, 1, N'Mensual',
     N'Repetir el mismo incidente es causa no resuelta, no mala suerte.',
     N'Exige causa raiz documentada antes de cerrar, no solo el registro cerrado.'),

    (N'com.backlog', N'Antiguedad del trabajo pendiente',
     N'Dias promedio que llevan abiertos los elementos no atendidos del equipo.',
     N'TrabajoPendiente', N'Comun', N'Automatico',
     N'Promedio de dias abiertos de los elementos vigentes', N'Dias', 10, 25, N'Bajar', 6, 1, N'Semanal',
     N'Backlog que crece significa que entra mas de lo que sale.',
     N'Compara entre responsables: si crece en los tres, es proceso o priorizacion, no una persona.'),

    (N'com.reincidencia', N'Reincidencia de problemas',
     N'Porcentaje de incidentes o tickets que repiten un tema ya atendido antes.',
     N'Reincidencia', N'Comun', N'Manual',
     N'Repetidos / Total * 100', N'Porcentaje', 10, 20, N'Bajar', 6, 1, N'Mensual',
     N'Alto significa que se atiende el sintoma y no la causa.',
     N'Vuelve obligatoria la causa raiz en toda reincidencia antes de cerrarla.'),

    (N'com.eficiencia', N'Eficiencia',
     N'Relacion entre el tiempo presupuestado y el tiempo realmente invertido en lo cerrado.',
     N'Eficiencia', N'Comun', N'Automatico',
     N'MinutosPresupuesto / MinutosInvertidos * 100', N'Porcentaje', 70, 50, N'Subir', 6, 1, N'Mensual',
     N'Baja con alto volumen indica exceso de interrupciones, no falta de esfuerzo.',
     N'Mide las interrupciones antes de concluir bajo desempeno.'),

    (N'com.disponibilidad', N'Disponibilidad de servicios',
     N'Porcentaje del tiempo que los servicios criticos estuvieron arriba durante el periodo.',
     N'Estabilidad', N'Comun', N'Automatico',
     N'(MinutosPeriodo - MinutosIndisponibilidad) / MinutosPeriodo * 100', N'Porcentaje', 99.5, 99, N'Subir', 6, 1, N'Mensual',
     N'Cuenta desde la falla real, no desde que alguien la reporta.',
     N'Si el tiempo de deteccion es largo, el problema es el monitoreo, no la respuesta.'),

    (N'com.documentacion', N'Documentacion',
     N'Porcentaje de procesos y entregables con documentacion vigente.',
     N'Documentacion', N'Comun', N'Manual',
     N'Documentados / Requeridos * 100', N'Porcentaje', 85, 60, N'Subir', 2, 1, N'Trimestral',
     N'No se nota hasta que algo falla y nadie mas sabe operarlo.',
     N'Si cae bajo presion de entrega constante, es senal de proceso, no de negligencia.'),

    (N'com.automatizacion', N'Automatizacion',
     N'Porcentaje de tareas repetitivas identificadas que ya estan automatizadas.',
     N'Automatizacion', N'Comun', N'Manual',
     N'Automatizadas / Automatizables * 100', N'Porcentaje', 50, 20, N'Subir', 2, 1, N'Trimestral',
     N'Bajo significa tiempo humano gastado en trabajo de maquina.',
     N'Bajo con carga alta es argumento real para invertir, no un deficit de la persona.'),

    (N'com.mejora', N'Mejora continua',
     N'Iniciativas de mejora propuestas e implementadas por el area en el periodo.',
     N'MejoraContinua', N'Comun', N'Manual',
     N'Conteo de mejoras implementadas', N'Conteo', 2, 0, N'Subir', 2, 1, N'Trimestral',
     N'Cero sostenido suele ser agotamiento operativo, no falta de ideas.',
     N'Revisa si el equipo tiene tiempo real para pensar o vive 100% reactivo.'),

    (N'com.satisfaccion', N'Satisfaccion del usuario',
     N'Calificacion promedio que dan los usuarios atendidos al cerrarse su ticket.',
     N'Satisfaccion', N'Comun', N'Automatico',
     N'Promedio de calificacion (1-5) de las encuestas del periodo', N'Indice', 4.3, 3.8, N'Subir', 6, 1, N'Mensual',
     N'Baja con SLA cumplido apunta a trato, no a tiempo.',
     N'Lee los comentarios cualitativos, no solo el numero.'),

    (N'com.prioridades', N'Gestion de prioridades',
     N'Porcentaje del trabajo ejecutado que estaba planeado, contra urgencias reactivas.',
     N'Prioridades', N'Comun', N'Manual',
     N'Planeado / Total ejecutado * 100', N'Porcentaje', 70, 50, N'Subir', 2, 1, N'Mensual',
     N'Bajo significa que la operacion vive apagando incendios.',
     N'Si es bajo en todo el equipo, el problema es la priorizacion gerencial.'),

    (N'com.colaboracion', N'Colaboracion entre areas',
     N'Porcentaje de solicitudes hacia otra area resueltas en el plazo acordado.',
     N'Colaboracion', N'Comun', N'Manual',
     N'CruzadasATiempo / CruzadasTotal * 100', N'Porcentaje', 85, 60, N'Subir', 2, 1, N'Mensual',
     N'Bajo y en un solo sentido indica dependencia mal gestionada.',
     N'Diferencia si el cuello de botella es de quien pide o de quien atiende.')

    /* ------------- Ambito DESARROLLO (11, pesos = 100) ------------- */
    INSERT INTO @Catalogo VALUES
    (N'dev.sprint', N'Cumplimiento de sprint',
     N'Porcentaje de historias comprometidas en la planeacion que se entregaron dentro del sprint.',
     N'Cumplimiento', N'Desarrollo', N'Automatico',
     N'Entregadas / Comprometidas * 100', N'Porcentaje', 85, 65, N'Subir', 15, 1, N'Mensual',
     N'Bajo sostenido indica compromisos mal dimensionados en planeacion.',
     N'Revisa como se estima y si el alcance se congela al iniciar el sprint.'),

    (N'dev.bugs.criticos', N'Incidentes criticos post-release',
     N'Incidentes de severidad alta o critica detectados en produccion despues de liberar.',
     N'Calidad', N'Desarrollo', N'Automatico',
     N'Conteo de incidentes severos ligados a releases del periodo', N'Conteo', 0, 1, N'Bajar', 15, 1, N'Mensual',
     N'Uno recurrente en el mismo componente es deuda tecnica, no descuido.',
     N'Exige causa raiz por incidente y revisa la cobertura de pruebas de ese componente.'),

    (N'dev.densidad.bugs', N'Densidad de defectos',
     N'Defectos encontrados por cada 10 historias liberadas.',
     N'Calidad', N'Desarrollo', N'Automatico',
     N'Correcciones / (HistoriasLiberadas / 10)', N'Indice', 2, 5, N'Bajar', 12, 1, N'Mensual',
     N'Alta apunta a requerimiento ambiguo o a pruebas insuficientes.',
     N'Revisa donde se escapan: analisis, desarrollo o QA.'),

    (N'dev.entregas.retrasadas', N'Entregas retrasadas',
     N'Porcentaje de releases que salieron despues de la fecha comprometida.',
     N'Puntualidad', N'Desarrollo', N'Automatico',
     N'ReleasesRetrasados / ReleasesTotales * 100', N'Porcentaje', 10, 30, N'Bajar', 10, 1, N'Mensual',
     N'Alto con sprint cumplido apunta al proceso de liberacion, no al desarrollo.',
     N'Separa el retraso de construccion del retraso de aprobacion o despliegue.'),

    (N'dev.retrabajo', N'Retrabajo de codigo',
     N'Porcentaje de historias reabiertas despues de marcarse como terminadas.',
     N'Calidad', N'Desarrollo', N'Automatico',
     N'Reabiertas / Terminadas * 100', N'Porcentaje', 8, 15, N'Bajar', 8, 1, N'Mensual',
     N'Alto suele venir de alcance que cambio a mitad de la historia.',
     N'Ajusta como se congela el alcance en la planeacion antes de tocar la ejecucion.'),

    (N'dev.deuda.tecnica', N'Deuda tecnica',
     N'Tamano y antiguedad del backlog tecnico pendiente de atender.',
     N'Calidad', N'Desarrollo', N'Manual',
     N'Porcentaje del backlog marcado como tecnico y su edad promedio', N'Porcentaje', 15, 30, N'Bajar', 8, 1, N'Trimestral',
     N'Crece silenciosamente y se cobra en incidentes de produccion.',
     N'Reserva capacidad fija por sprint para deuda, no la dejes al sobrante.'),

    (N'dev.cobertura.pruebas', N'Cobertura de pruebas automatizadas',
     N'Porcentaje de la funcionalidad critica cubierta por pruebas automatizadas.',
     N'Calidad', N'Desarrollo', N'Manual',
     N'Cubierto / Total critico * 100', N'Porcentaje', 60, 30, N'Subir', 8, 1, N'Trimestral',
     N'Baja convierte cada release en una apuesta.',
     N'Prioriza cobertura en los componentes con mas incidentes, no en todo por parejo.'),

    (N'dev.tiempo.bugs', N'Tiempo de resolucion de defectos',
     N'Horas laborables promedio desde que se reporta un defecto hasta que se cierra.',
     N'Tiempos', N'Desarrollo', N'Automatico',
     N'Promedio de horas laborables de las correcciones cerradas', N'Horas', 16, 40, N'Bajar', 8, 1, N'Mensual',
     N'Alto indica que los defectos compiten con trabajo nuevo sin prioridad clara.',
     N'Define una politica de atencion de defectos por severidad.'),

    (N'dev.mtto.vs.nuevo', N'Mantenimiento contra desarrollo nuevo',
     N'Porcentaje del tiempo del equipo dedicado a correccion y soporte en vez de construir capacidades nuevas.',
     N'Eficiencia', N'Desarrollo', N'Automatico',
     N'MinutosMantenimiento / MinutosTotales * 100', N'Porcentaje', 30, 50, N'Bajar', 6, 1, N'Mensual',
     N'Arriba de 50% sostenido el equipo ya no construye, solo sostiene.',
     N'Es senal de calidad acumulada: ataca la causa, no pidas mas velocidad.'),

    (N'dev.cicd', N'Automatizacion de despliegue',
     N'Porcentaje de despliegues ejecutados por pipeline sin intervencion manual.',
     N'Automatizacion', N'Desarrollo', N'Manual',
     N'Automatizados / Total * 100', N'Porcentaje', 80, 40, N'Subir', 5, 1, N'Trimestral',
     N'Bajo multiplica el riesgo de error humano en cada liberacion.',
     N'Automatiza primero el despliegue mas frecuente, no el mas complejo.'),

    (N'dev.documentacion', N'Documentacion tecnica',
     N'Porcentaje de funcionalidades liberadas con documentacion o notas de version.',
     N'Documentacion', N'Desarrollo', N'Manual',
     N'Documentadas / Liberadas * 100', N'Porcentaje', 90, 60, N'Subir', 5, 1, N'Trimestral',
     N'Bajo encarece cada cambio futuro sobre esa funcionalidad.',
     N'Haz la nota de version parte de la definicion de terminado, no un paso aparte.')

    /* --------- Ambito INFRAESTRUCTURA (12 = 100, +1 diagnostico) --------- */
    INSERT INTO @Catalogo VALUES
    (N'inf.disponibilidad', N'Disponibilidad de servicios criticos',
     N'Porcentaje de tiempo arriba de los servicios que no pueden caer.',
     N'Estabilidad', N'Infraestructura', N'Automatico',
     N'(MinutosPeriodo - MinutosIndisponibilidad) / MinutosPeriodo * 100', N'Porcentaje', 99.5, 99, N'Subir', 15, 1, N'Mensual',
     N'Cada decima abajo de la meta son horas de operacion detenida.',
     N'Distingue caidas por cambio propio de caidas por falla de plataforma.'),

    (N'inf.incidentes.severidad', N'Incidentes por severidad',
     N'Cantidad de incidentes criticos y mayores registrados en el periodo.',
     N'Incidencias', N'Infraestructura', N'Automatico',
     N'Conteo de incidentes de severidad alta del periodo', N'Conteo', 0, 2, N'Bajar', 12, 1, N'Mensual',
     N'Dos del mismo componente no son dos incidentes, es uno sin resolver.',
     N'Agrupa por componente antes de contar: revela la causa comun.'),

    (N'inf.backups', N'Cumplimiento de respaldos',
     N'Porcentaje de respaldos ejecutados y con restauracion probada.',
     N'Continuidad', N'Infraestructura', N'Manual',
     N'RespaldosOk / RespaldosProgramados * 100', N'Porcentaje', 100, 95, N'Subir', 10, 1, N'Mensual',
     N'Un respaldo que nunca se restauro no es un respaldo, es una suposicion.',
     N'Exige una prueba de restauracion real por trimestre, no solo la ejecucion.'),

    (N'inf.mttr', N'Tiempo medio de resolucion',
     N'Horas promedio desde que se detecta un incidente hasta que se resuelve.',
     N'Tiempos', N'Infraestructura', N'Automatico',
     N'Promedio de horas entre deteccion y resolucion', N'Horas', 4, 12, N'Bajar', 10, 1, N'Mensual',
     N'Alto con deteccion rapida apunta a falta de procedimiento o de accesos.',
     N'Documenta el procedimiento de los tres incidentes mas frecuentes.'),

    (N'inf.cambios.fallidos', N'Cambios fallidos',
     N'Porcentaje de despliegues que terminaron en fallo.',
     N'Calidad', N'Infraestructura', N'Automatico',
     N'DesplieguesFallidos / DesplieguesTotales * 100', N'Porcentaje', 5, 15, N'Bajar', 8, 1, N'Mensual',
     N'Alto indica que se despliega sin ventana de validacion ni plan de reversa.',
     N'Exige plan de reversa probado antes de autorizar el cambio.'),

    (N'inf.monitoreo', N'Cobertura de monitoreo',
     N'Porcentaje de activos criticos con monitoreo y alertamiento activo.',
     N'Prevencion', N'Infraestructura', N'Manual',
     N'Monitoreados / Criticos * 100', N'Porcentaje', 100, 80, N'Subir', 8, 1, N'Trimestral',
     N'Lo que no se monitorea se entera el usuario antes que el area.',
     N'Empieza por los activos que ya causaron un incidente.'),

    (N'inf.mttd', N'Tiempo medio de deteccion',
     N'Minutos entre la falla real y su deteccion.',
     N'Prevencion', N'Infraestructura', N'Automatico',
     N'Promedio de minutos entre ocurrencia y deteccion', N'Conteo', 5, 30, N'Bajar', 8, 1, N'Mensual',
     N'Alto significa que la falla la reporta el usuario, no el monitoreo.',
     N'Si es alto, el problema es la cobertura de monitoreo, no la respuesta.'),

    (N'inf.recurrentes', N'Problemas recurrentes',
     N'Porcentaje de incidentes que repiten uno ya resuelto antes.',
     N'Reincidencia', N'Infraestructura', N'Manual',
     N'Repetidos / Total * 100', N'Porcentaje', 10, 20, N'Bajar', 8, 1, N'Mensual',
     N'Alto significa que se restablece el servicio pero no se elimina la causa.',
     N'Separa "restaurar servicio" de "cerrar el problema": son dos cierres distintos.'),

    (N'inf.preventivo', N'Mantenimiento preventivo cumplido',
     N'Porcentaje de mantenimientos programados que se ejecutaron a tiempo.',
     N'Prevencion', N'Infraestructura', N'Manual',
     N'Ejecutados / Programados * 100', N'Porcentaje', 95, 70, N'Subir', 6, 1, N'Mensual',
     N'Bajo casi siempre es sintoma de sobrecarga reactiva, no de descuido.',
     N'Bloquea horas fijas de preventivo en la agenda antes de pedir el cumplimiento.'),

    (N'inf.capacidad', N'Capacidad de infraestructura',
     N'Uso de los recursos criticos contra su umbral de riesgo.',
     N'Capacidad', N'Infraestructura', N'Manual',
     N'Uso maximo sostenido de CPU, disco, memoria o licencias', N'Porcentaje', 70, 85, N'Bajar', 6, 1, N'Mensual',
     N'Arriba del umbral cualquier pico se vuelve una caida.',
     N'Convierte la proyeccion de capacidad en presupuesto, no en alerta recurrente.'),

    (N'inf.seguridad', N'Hallazgos de seguridad abiertos',
     N'Hallazgos criticos de seguridad sin remediar y su antiguedad.',
     N'Seguridad', N'Infraestructura', N'Manual',
     N'Conteo de hallazgos criticos con mas de 30 dias abiertos', N'Conteo', 0, 1, N'Bajar', 6, 1, N'Mensual',
     N'Un hallazgo critico envejecido es una decision, no un pendiente.',
     N'Ponle fecha compromiso a cada hallazgo critico o acepta el riesgo por escrito.'),

    (N'inf.automatizacion', N'Automatizacion de la operacion',
     N'Porcentaje de tareas operativas repetitivas resueltas con script o automatizacion.',
     N'Automatizacion', N'Infraestructura', N'Manual',
     N'Automatizadas / Total repetitivas * 100', N'Porcentaje', 50, 20, N'Subir', 3, 1, N'Trimestral',
     N'Bajo mantiene al equipo ocupado en trabajo que no requiere criterio.',
     N'Automatiza la tarea mas frecuente del mes, medida en horas, no en cantidad.'),

    (N'inf.tiempo.reactivo', N'Tiempo en soporte reactivo',
     N'Porcentaje del tiempo del area dedicado a atender solicitudes en vez de proyectos y preventivo. Es senal diagnostica, no una meta a perseguir: por eso no puntua en el score.',
     N'Diagnostico', N'Infraestructura', N'Automatico',
     N'MinutosEnTickets / MinutosTotales * 100', N'Porcentaje', 35, 55, N'Bajar', 0, 0, N'Mensual',
     N'Arriba de 55% el area ya no hace prevencion, solo apaga incendios.',
     N'No lo trates como bajo desempeno: es evidencia de dimensionamiento insuficiente.')

    /* --------------- Ambito SOPORTE (10, pesos = 100) --------------- */
    INSERT INTO @Catalogo VALUES
    (N'sop.sla', N'Cumplimiento de SLA',
     N'Porcentaje de tickets resueltos dentro del plazo pactado en su acuerdo de servicio.',
     N'SLA', N'Soporte', N'Automatico',
     N'TicketsEnSla / TicketsConSla * 100', N'Porcentaje', 90, 75, N'Subir', 18, 1, N'Mensual',
     N'Bajo concentrado en una categoria casi nunca es la persona: es esa categoria.',
     N'Desglosa por categoria antes de concluir: el patron senala la causa.'),

    (N'sop.primera.respuesta', N'Tiempo de primera respuesta',
     N'Minutos promedio desde que entra el ticket hasta el primer contacto con el usuario.',
     N'Tiempos', N'Soporte', N'Automatico',
     N'Promedio de minutos entre registro y primera respuesta', N'Conteo', 30, 240, N'Bajar', 12, 1, N'Mensual',
     N'Alto deteriora la percepcion aunque la resolucion final sea buena.',
     N'Es el indicador mas barato de mejorar: casi siempre es asignacion, no capacidad.'),

    (N'sop.tiempo.resolucion', N'Tiempo de resolucion',
     N'Horas promedio desde el registro del ticket hasta su resolucion.',
     N'Tiempos', N'Soporte', N'Automatico',
     N'Promedio de horas entre registro y resolucion', N'Horas', 8, 24, N'Bajar', 12, 1, N'Mensual',
     N'Alto con primera respuesta buena apunta a dependencias, no a atencion.',
     N'Mide cuanto de ese tiempo fue espera de un tercero antes de actuar.'),

    (N'sop.vencidos', N'Tickets vencidos',
     N'Porcentaje de los tickets abiertos que ya rebasaron su plazo de resolucion.',
     N'TrabajoPendiente', N'Soporte', N'Automatico',
     N'TicketsVencidos / TicketsAbiertos * 100', N'Porcentaje', 5, 15, N'Bajar', 12, 1, N'Semanal',
     N'Concentrados en un tema significan un bloqueo, no lentitud generalizada.',
     N'Agrupa los vencidos por categoria: si se concentran, ataca ese tramite.'),

    (N'sop.reabiertos', N'Tickets reabiertos',
     N'Porcentaje de tickets cerrados que el usuario volvio a abrir.',
     N'Calidad', N'Soporte', N'Automatico',
     N'Reabiertos / Cerrados * 100', N'Porcentaje', 8, 18, N'Bajar', 10, 1, N'Mensual',
     N'Alto revela cierres prematuros: el conteo de cerrados esta inflado.',
     N'Nunca leas "tickets cerrados" sin este indicador al lado.'),

    (N'sop.csat', N'Satisfaccion del usuario',
     N'Calificacion promedio de la encuesta que se envia al cerrar el ticket.',
     N'Satisfaccion', N'Soporte', N'Automatico',
     N'Promedio de calificacion (1-5) de las encuestas del periodo', N'Indice', 4.3, 3.8, N'Subir', 10, 1, N'Mensual',
     N'Baja con SLA cumplido apunta a comunicacion, no a tiempos.',
     N'Lee los comentarios: el numero solo dice que hay un problema, no cual.'),

    (N'sop.recurrentes', N'Incidentes recurrentes por categoria',
     N'Porcentaje de tickets que repiten un tema ya atendido antes.',
     N'Reincidencia', N'Soporte', N'Manual',
     N'Repetidos / Total * 100', N'Porcentaje', 10, 20, N'Bajar', 8, 1, N'Mensual',
     N'Alto significa demanda evitable consumiendo capacidad todos los meses.',
     N'Un tema recurrente se resuelve con capacitacion o autoservicio, no atendiendolo mejor.'),

    (N'sop.primer.nivel', N'Resolucion en primer nivel',
     N'Porcentaje de tickets resueltos sin escalar a otro nivel u otra area.',
     N'Eficiencia', N'Soporte', N'Manual',
     N'ResueltosNivel1 / Total * 100', N'Porcentaje', 60, 35, N'Subir', 8, 1, N'Mensual',
     N'Bajo carga a las otras areas con trabajo que pudo quedarse en la mesa.',
     N'Revisa que tipo de ticket se escala siempre: ahi hay una capacitacion pendiente.'),

    (N'sop.distribucion', N'Distribucion de carga entre agentes',
     N'Dispersion de tickets por agente contra el promedio del equipo.',
     N'Prioridades', N'Soporte', N'Automatico',
     N'Desviacion de tickets por agente contra el promedio', N'Porcentaje', 25, 50, N'Bajar', 5, 1, N'Mensual',
     N'Alta indica que uno absorbe lo dificil mientras otros quedan holgados.',
     N'Revisa si es especializacion real o asignacion por costumbre.'),

    (N'sop.autoservicio', N'Demanda resoluble por capacitacion o autoservicio',
     N'Porcentaje de tickets recurrentes que podrian evitarse con base de conocimiento o capacitacion.',
     N'MejoraContinua', N'Soporte', N'Manual',
     N'TicketsEvitables / Total * 100', N'Porcentaje', 15, 30, N'Bajar', 5, 1, N'Trimestral',
     N'Creciente significa que la mesa absorbe un problema de formacion.',
     N'Convierte los tres temas mas repetidos en articulos de la base de conocimiento.')

    /* ---------------- Insercion idempotente por Clave ---------------- */
    INSERT INTO dbo.tblIndicadorGestion
        (Clave, Nombre, Descripcion, Categoria, Ambito, Origen, Formula, Unidad,
         Meta, UmbralAlerta, Direccion, Peso, PonderaEnScore, Periodicidad,
         InterpretacionMala, AccionSugerida, UsuarioRegistro, Activo)
    SELECT c.Clave, c.Nombre, c.Descripcion, c.Categoria, c.Ambito, c.Origen, c.Formula, c.Unidad,
           c.Meta, c.UmbralAlerta, c.Direccion, c.Peso, c.PonderaEnScore, c.Periodicidad,
           c.InterpretacionMala, c.AccionSugerida, N'script-despliegue', 1
    FROM @Catalogo c
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblIndicadorGestion i WHERE i.Clave = c.Clave)

    DECLARE @Sembrados INT = @@ROWCOUNT
    PRINT 'OK: indicadores sembrados -> ' + CAST(@Sembrados AS NVARCHAR(10))

    /* Verificacion: los pesos de cada ambito deben sumar 100 (los que puntuan). */
    DECLARE @Ambito NVARCHAR(30), @Suma DECIMAL(9,4)
    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Ambito, SUM(Peso) FROM dbo.tblIndicadorGestion
        WHERE Activo = 1 AND PonderaEnScore = 1 GROUP BY Ambito
    OPEN cur
    FETCH NEXT FROM cur INTO @Ambito, @Suma
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @Suma <> 100
            PRINT 'AVISO: los pesos del ambito ' + @Ambito + ' suman ' + CAST(@Suma AS NVARCHAR(20)) + ', no 100'
        ELSE
            PRINT 'OK: pesos del ambito ' + @Ambito + ' suman 100'
        FETCH NEXT FROM cur INTO @Ambito, @Suma
    END
    CLOSE cur
    DEALLOCATE cur

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
