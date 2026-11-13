# YoshiSQL - Contexto para Claude

Entorno gráfico de escritorio (alternativa ligera a SSMS) para **SQL Server en Linux y Windows**.
C# + .NET 10 + Avalonia 12 (MVVM con CommunityToolkit.Mvvm). Versión actual: **0.5.0**.

Documentos de referencia en la raíz: `README.md` (funciones y uso) y `ESTRUCTURA_YOSHISQL.txt`
(arquitectura, convenciones y estado detallado). Ojo: ese archivo menciona una carpeta `docs/`
que **no existe**; toda la documentación vive en esos dos archivos.

## Comandos

```bash
dotnet build                                   # compilar (advertencias = errores)
dotnet test                                    # 289 pruebas xUnit
dotnet run --project src/YoshiSQL.Escritorio   # ejecutar la app
```

En este equipo (Windows 11) hay .NET SDK 9 y 10 instalados.

## Arquitectura (las dependencias apuntan hacia el Dominio)

| Proyecto | Rol | Regla |
|---|---|---|
| `src/YoshiSQL.Dominio` | Entidades, enums y contratos (`Contratos/I*.cs`) | No depende de nada |
| `src/YoshiSQL.Aplicacion` | Servicios / casos de uso (`Servicio*`) | Solo conoce al Dominio |
| `src/YoshiSQL.Infraestructura` | SQL Server (SqlClient + ScriptDom), JSON en disco, cifrado, exportadores | Implementa contratos del Dominio |
| `src/YoshiSQL.Escritorio` | UI Avalonia: `Vistas/*.axaml` + `ModelosDeVista/` + `Servicios/` | Nunca escribe SQL |
| `pruebas/*.Pruebas` | xUnit por capa | Nombres `Metodo_Escenario_Resultado` |

- Inyección de dependencias: `RegistroDeServicios.cs` (Aplicacion), `RegistroDeInfraestructura.cs`, `ContenedorDeDependencias.cs` (Escritorio).
- El SQL de catálogo vive en `Infraestructura/SqlServer/ConsultasDeCatalogo/*.sql` (recursos embebidos), no en strings de C#.
- Punto central del motor: `IProveedorDeBaseDeDatos` (Explorador, Ejecutor, DivisorDeLotes, GeneradorDeScripts...).
- Cada tipo de pestaña = un `DocumentoModeloDeVista` + su vista (consulta, diagrama, diseño de tabla, edición de filas, monitor).
- La ventana principal está en `VentanaPrincipalModeloDeVista.cs` + `.Sesion.cs` (partial).
- Menú contextual del árbol: `ModelosDeVista/Explorador/FabricaDeNodos.cs` → llama a `IAccionesDelExplorador` (implementado por la ventana principal).

## Convenciones (obligatorias)

- **Todo en español** (clases, métodos, variables, comentarios), **sin tildes ni ñ en nombres de código**
  (`Pestana`, `Diseno`, `Funcion`). En textos visibles al usuario sí van tildes.
- Se conservan: sufijo `Async`, prefijo `I` en interfaces. Métodos async reciben `CancellationToken tokenDeCancelacion`.
- Métodos empiezan con verbo en infinitivo; booleanos `Es/Esta/Tiene/Puede`; colecciones en plural; sin abreviaturas.
- Campos privados `_camelCase`; una clase por archivo; namespaces por archivo; llaves siempre.
- Nada de números mágicos (constantes con nombre). Comentarios explican el **por qué**.
- `TreatWarningsAsErrors` y `EnforceCodeStyleInBuild` activos: el estilo del `.editorconfig` rompe la compilación.
- Errores: se registran con `IServicioDeErrores` (código de error + Serilog), nunca se cierra la app.
- Commits en español, cortos, en infinitivo ("Agregar...", "Corregir...").

## Estado actual (revisado 2026-10-05)

Implementado: conexiones con contraseña cifrada (DPAPI en Windows), explorador tipo SSMS, editor con
resaltado/autocompletado/formato/buscar, ejecución por lotes `GO`, grilla de resultados (solo lectura),
exportar CSV/JSON/Excel, historial, plan de ejecución estimado/real, diseñador de tablas, diagramas,
monitor de actividad, respaldo/restauración, preferencias de tema, autoguardado y recuperación.

Último commit: "Compatibilidad con Windows 11". Este repo es una **copia clonada en Windows**; el desarrollo
original se hizo en Linux (Ubuntu 24.04). La versión de Windows aún **no está probada a fondo**.

### Ver / editar datos de tablas (siguiente foco del usuario)

Cómo funciona hoy:
- "Seleccionar las primeras 1000 filas" → abre una pestaña de consulta con un `SELECT TOP` y la ejecuta (grilla de solo lectura, `Vistas/Resultados/GrillaDeResultados.cs`).
- "Editar las primeras 200 filas" → pestaña de edición:
  - `Aplicacion/EdicionDeFilas/ServicioDeEdicionDeFilas.cs` (constante `CantidadDeFilasAEditar = 200`)
  - `Escritorio/ModelosDeVista/EdicionDeFilas/PestanaDeEdicionDeFilasModeloDeVista.cs` y `FilaEditableModeloDeVista.cs`
  - `Escritorio/Vistas/EdicionDeFilas/GrillaDeEdicion.cs` (DataGrid construido en código)
  - SQL generado en `GeneradorDeScriptsSqlServer.GenerarComandosDeEdicion` y valores convertidos en `Infraestructura/SqlServer/ConvertidorDeValoresDeEdicion.cs`
  - Guarda todos los cambios juntos con un botón, en una transacción, con parámetros tipados.

Limitaciones detectadas frente a SSMS:
- Solo 200 filas fijas; sin paginación, sin cambiar la cantidad ni editar el `WHERE/ORDER BY` (panel SQL de SSMS).
- Tablas sin llave primaria → solo lectura.
- Sin ordenar ni filtrar columnas en la grilla de edición.
- Se escribe `NULL` como texto para nulo (no hay Ctrl+0 como en SSMS).
- Binarios, identidad y rowversion son solo lectura; no hay editor especial para bit/fechas.
- SSMS guarda al salir de la fila; aquí se guarda todo junto con "Guardar cambios".
- Vistas no se pueden editar.

### Problemas encontrados en Windows (2026-10-05)
- CORREGIDO (sin commit aún): el menú del clic derecho del explorador nunca aparecía. En Avalonia 12, durante
  `ContextMenu.Opening` el menú aún no tiene DataContext (ItemCount = 0), y el código cancelaba siempre.
  Ahora se decide en `ContextRequested` en fase túnel sobre el TreeView (`PanelDelExplorador.axaml.cs`).
- CORREGIDO (sin commit aún): "Formatear SQL" dejaba líneas en blanco de más en Windows porque ScriptDom genera `\r\n`
  y las regex esperan `\n`. `FormateadorDeSqlTSql` ahora normaliza a `\n` y devuelve con `Environment.NewLine`.
- CORREGIDO: la prueba de permisos Unix usa `[FactFueraDeWindows]` (se omite en Windows, corre en Linux).
- Resultado en Windows: 128 pruebas correctas, 1 omitida. Para pruebas que dependan del SO usar ese atributo.
- Al trabajar en Windows: el código puede generar `\r\n`; no asumir `\n` en regex o `Split`.

### Bug corregido (2026-10-25): scripts con varios procedimientos separados por GO
`DivisorDeLotesTSql` usaba el lexer de ScriptDom y, ante CUALQUIER error léxico (un `` ` ``, un `?`, un `/*` sin
cerrar), mandaba todo el script como un solo lote → cada `CREATE PROCEDURE` fallaba ("must be the first statement
in a query batch"). Además, saltos de línea solo `\r` lanzaban `ArgumentOutOfRangeException`. Ahora separa línea
a línea como SSMS/sqlcmd (GO solo en su línea, fuera de textos, `[identificadores]` y comentarios anidados).
También se fijó `System.Security.Cryptography.Pkcs` 10.0.13: la 9.0.18 que trae SqlClient rompía `dotnet build` (NU1902/NU1903).

### Cacería de bugs (2026-10-25) — corregidos, sin commit
- Reconexión: si se caía la conexión, la pestaña se reabría en la base INICIAL de la cadena de conexión, no en la
  que el usuario usaba (USE / lista) → el script corría en otra base. `SesionDeConsultaSqlServer._baseDeDatosEnUso`.
  Cambiar de base mientras se ejecuta ahora da un mensaje claro.
- Edición de filas: una celda editada y devuelta a su valor original generaba `UPDATE ... SET  WHERE` (inválido) y la
  transacción descartaba TODOS los cambios. Ahora se omite (`ObtenerPosicionesModificadas`).
- Edición de filas: `decimal` con `NumberStyles.Number` leía "10,5" como 105 (coma = miles) → dato corrupto. Ahora se
  rechaza con sugerencia de usar punto; fechas con "/" (ambiguas dd/MM vs MM/dd) se rechazan pidiendo aaaa-mm-dd.
- Exportar a script / Copiar como INSERT: fechas como `'yyyy-MM-dd HH:mm...'` las lee mal un `datetime` con idioma
  español (DATEFORMAT dmy). Ahora ISO 8601 con `T`. LIMITACIÓN: sigue con 3 decimales (datetime2(7) pierde sub-ms).
- `conexiones.json` dañado impedía listar y guardar conexiones (no se podía conectar). Ahora se copia a
  `conexiones.json.danado` y se empieza vacío.
- Cerrar la app: un fallo al liberar una pestaña impedía cerrar la ventana; ahora se registra y se cierra.
- NUEVO: "Eliminar..." en el menú de la base (`DialogoDeEliminacionDeBaseDeDatos` + `ServicioDeEliminacionDeBaseDeDatos`).
  Libera el pool de conexiones del explorador (`IEjecutorDeConsultas.LiberarConexionesInactivas`, que si no dejaba
  sesiones "dentro" de la base y el DROP fallaba por culpa de YoshiSQL), pasa a master las pestañas en esa base, y
  con "cerrar conexiones" usa SINGLE_USER en TRY/CATCH que vuelve a MULTI_USER si el DROP falla (antes la base podía
  quedar atrapada en SINGLE_USER).
### Bugs que inutilizaban el sistema (2026-10-25, segunda ronda) — corregidos, sin commit
- `DetectorDeCambiosDeEsquema`: la regex `SELECT[\s\S]*?INTO` era cuadrática y corría en el hilo de la UI tras cada
  ejecución (medido: 8.000 SELECT = 77 s congelado). Ahora es lineal (INTO no precedido de INSERT/MERGE) + timeout.
- Resultados: `TextoDeResultados` se armaba en cada ejecución aunque no se viera; ahora solo en modo texto y con
  tope de 5.000 filas. Máximo 50 grillas por ejecución (`MaximoDeConjuntosVisibles`), con aviso en Mensajes.
- Conexión: un fallo al guardar perfil/contraseña ya no hace fallar la conexión (`RecordarParaLaProximaVezAsync`).
  `AlmacenDeCredencialesCifrado` se recupera solo: credenciales.json dañado → copia .danado; clave ilegible (DPAPI de
  otro equipo/usuario) → clave nueva.
- Edición de filas: el conteo usa `SET NOCOUNT ON` + `SELECT @@ROWCOUNT` (los disparadores sin NOCOUNT hacían fallar
  "1 fila esperada"); `ParametroSql.TipoEnElServidor` envía las fechas con su tipo exacto (datetime2 ya no se redondea);
  `Columna.EsCalculada` (is_computed en el catálogo) → columnas calculadas de solo lectura y fuera del INSERT exportado
  (también rowversion). PENDIENTE verificar con servidor real: @@ROWCOUNT con disparadores INSTEAD OF.
- Autocompletado: analiza solo el lote del cursor (`RecortarAlLoteDelCursor`).
- Respaldo: "Comprimir" solo por defecto si la edición lo admite (`ServicioDeRespaldos.AdmiteCompresion`; Express/Web no).
  Restaurar libera antes el pool del explorador. Salud: consultas con 600 s de límite (antes 30 s).
- PENDIENTE (de la auditoría): exportación completa (esquemas, tipos de usuario, dependencias, encoding), Excel con bigint,
  diseñador con precisión de datetime2/time, concurrencia optimista al editar filas.

- Resultados enormes: desde 2026-11-10 hay un tope de 1.000.000 de filas por resultado (siguen en memoria, sin streaming).

### Revisión contra SQL Server real (2026-11-09) — corregido, sin commit
En este equipo hay un SQL Server 2022 local (`localhost\SQL2022`, autenticación de Windows) con bases reales del
usuario. Se probó de punta a punta: exportar cada base → ejecutar el script en una base nueva → comparar objetos y datos.
- **Exportar toda la base ahora produce una copia que funciona** (antes se creaba "sin errores" pero sin DEFAULT, CHECK,
  UNIQUE, índices, disparadores, secuencias, tipos tabla, columnas calculadas ni cascadas; en PuntoVentaDB fallaban
  12 procedimientos). `IExploradorDeEsquema.ObtenerDetallesDelEsquemaAsync` → `DetallesDelEsquema` (una consulta de
  catálogo por pieza); el orden de las secciones está explicado en `ServicioDeExportacionDeBaseDeDatos` y NO es libre.
  `OrdenadorPorDependencias` ordena funciones y vistas. Datos en lotes de 1.000 filas con `SET NOCOUNT ON`.
  Resultado: las 8 bases propias se recrean con 0 errores e idénticos objetos y datos. Límites: ver B-62, B-63, B-64.
- **Abrir .sql en ANSI** (lo habitual en SSMS/Windows): `DetectorDeCodificacion` (BOM → UTF-8 válido → UTF-16 sin
  marca → Windows-1252). Antes las tildes y eñes de nombres de columnas y textos llegaban rotas y el script fallaba.
- Errores dentro de un procedimiento ejecutado: el mensaje dice "Procedimiento X, Línea N" (la línea es del
  procedimiento, no del editor). `SesionDeConsultaSqlServer.ConvertirEnMensaje`.
- Comprobado sin cambios: el divisor de GO con los 836 módulos reales, edición de filas con disparadores
  (AFTER sin NOCOUNT e INSTEAD OF), respaldo, restauración y eliminar base con sesiones abiertas.
- Para repetir estas pruebas: armar una consola temporal (fuera del repo) que use `AgregarInfraestructura()` +
  `AgregarAplicacion()` y cree bases `ZZ_YoshiPrueba_*` que se borran al terminar. Nunca tocar las bases del usuario.

### Limpieza de `BUGS.md` (2026-11-10) — corregido, sin commit
Se arreglaron todos los 🟠 y casi todos los 🟡 (queda solo B-63). Lo que cambia cómo se trabaja:
- **Exportar** ya no arma el script en memoria: `ExportarAsync` escribe a un `TextWriter` y lee las filas con
  `IEjecutorDeConsultas.LeerFilasAsync` (IAsyncEnumerable). La UI usa `IServicioDeDialogos.EjecutarConProgresoAsync`
  (`DialogoDeProgreso`: avance + Cancelar), reutilizable para cualquier tarea larga.
- **Editar filas**: el UPDATE compara también el valor original de las columnas cambiadas (concurrencia optimista).
  Las pruebas de `GenerarComandosDeEdicion` esperan `... WHERE [Id] = @k0 AND [Col] = @o0`.
- **Sesión**: `ISesionDeConsulta.MensajeRecibido` (mensajes en vivo; puede llegar desde otro hilo) y
  `TieneTransaccionAbiertaAsync` (aviso al cerrar pestaña).
- **Segunda ventana de YoshiSQL**: empieza vacía y NO guarda sesión (`IRepositorioDeSesion.EsInstanciaSecundaria`).
- **Tipos de usuario fuera de dbo** llegan del catálogo como `esquema.Tipo` (`TipoDeDato` admite un punto).
- **CSV**: a los textos que empiezan con `= + - @` se les antepone un apóstrofo (inyección de fórmulas).
- **Diseñador**: `DROP COLUMN` quita antes el DEFAULT; `ADD ... NOT NULL` usa un DEFAULT temporal; el script termina
  con `SET XACT_ABORT OFF` en un lote aparte.
- Muchos arreglos de interfaz están "por verificar en la aplicación" (ver cada fila en `BUGS.md`).

### Mejoras de interfaz (2026-11-10) — hecho, sin commit
Revisión visual con capturas de la ventana real. Lo que cambia cómo se trabaja en la interfaz:
- **Disposición recordada**: `DisposicionDeLaVentana` (Dominio) + `IRepositorioDeDisposicion` → `ventana.json` en la
  carpeta de estado + `DisposicionDeLaInterfaz` (Escritorio, singleton observable). Guarda tamaño, maximizada, ancho del
  explorador, alto de resultados y si esos dos paneles se ven. La ventana la aplica al abrir y la guarda al cerrar; la
  vista de consulta la obtiene de `VentanaPrincipalModeloDeVista.Disposicion`.
- **Paneles ocultables**: Ctrl+B (explorador) y Ctrl+R (resultados), menú "Ver" y botones en la barra. Al ejecutar con
  el panel de resultados oculto, se vuelve a mostrar solo.
- **Separadores**: `GridSplitter` transparente de 5 px + `Border.LineaDivisoria` de 1 px. No volver a darle fondo.
- **Pestañas**: clic derecho (cerrar, cerrar las demás, cerrar todas), clic con la rueda para cerrar, punto verde mientras
  ejecuta (`DocumentoModeloDeVista.EstaOcupado`) y servidor en la ayuda emergente (`DescripcionParaLaPestana`).
- **Editor**: línea y columna en la barra de estado (`EditorSql.PosicionDelCursor`); fragmentos con Tab
  (`FragmentosDeTSql`: ssf, st, ins, upd, del, ct, cte, decl, ifex, try, tran, proc, func, cur; lista en Ayuda);
  subrayado de errores de sintaxis en vivo (`SubrayadoDeErrores` + `IFormateadorDeSql.BuscarErroresDeSintaxis`,
  espera 700 ms, se puede apagar en Preferencias).
- **Buscar en la base de datos** (Ctrl+Shift+O): `BuscarObjetos.sql` busca en nombres, columnas y código; los resultados
  se muestran reutilizando la paleta de comandos.
- **Resultados**: clic en el encabezado ordena (`ComparadorDeCeldas`, solo en pantalla).
- **Conectar**: nombre opcional de la conexión (`PerfilDeConexion.Nombre`/`Titulo`/`Detalle`), "Probar conexión" y
  botón para ver la contraseña.
- Para capturar la ventana en pruebas: `PrintWindow` sobre el identificador de la ventana, NUNCA copiar la pantalla
  (captura lo que el usuario tenga delante). En Windows la carpeta de configuración no se puede redirigir: una copia de
  prueba usa las conexiones reales del usuario, así que no conectar desde ella.
- Segunda tanda (2026-11-10): filtro en la grilla de resultados (menú "Filtrar filas...", `DataGridCollectionView.Filter`),
  alto de cada grilla según sus filas cuando hay varias (`AltoSugerido`), separadores en el menú de las tablas
  (`AccionDelNodo.Separador()`: Avalonia dibuja como línea una opción cuyo texto es "-"), nombres accesibles en los
  botones de solo icono (`AutomationProperties.Name`), **valores DEFAULT en el diseñador**
  (`DefinicionDeColumna.ValorPredeterminado`, `CambiosDeTabla.ColumnasConOtroValorPredeterminado`,
  `ListarValoresPredeterminadosDeTabla.sql`), **tope de 1.000.000 de filas por resultado**
  (`SesionDeConsultaSqlServer.MaximoDeFilasPorResultado`: corta el lote con `comando.Cancel()` y avisa),
  fechas `datetime2`/`datetimeoffset` con sus 7 decimales al exportar, propiedades extendidas (MS_Description)
  en la exportación, y `empaquetar-linux.sh` (genera un .deb; **sin probar**, se escribió desde Windows).
- Tercera tanda (2026-11-10): botones "Subir"/"Bajar" en el diseñador (solo al crear la tabla: SQL Server no reordena
  columnas de una tabla existente), grupo opcional de las conexiones (`PerfilDeConexion.Grupo`; la lista las muestra
  juntas y el título queda "Grupo / Nombre") y disparadores de base de datos (DDL) en la exportación, al final de todo.
- Cuarta tanda (2026-11-12): exportar incluye índices columnstore (`IndiceDeTabla.EsColumnar`) y, como opción del
  diálogo, usuarios, roles y permisos (`OpcionesDeExportacion.UsuariosYPermisos`, `IExploradorDeEsquema.ObtenerSeguridadAsync`
  → `SeguridadDeLaBaseDeDatos`; un usuario se crea `FOR LOGIN` solo si ese inicio existe en el destino y, si no,
  `WITHOUT LOGIN`). Iconos en el menú del explorador (`IconosDeAcciones`: se eligen por una palabra del texto de la
  opción). Los diálogos redimensionables recuerdan su tamaño (`ServicioDeDialogos.ConTamanoRecordado` +
  `DisposicionDeLaVentana.Dialogos`). El historial marca los scripts recortados (B-65).
- **Probar la interfaz sin tocar los datos del usuario**: variable de entorno `YOSHISQL_CARPETA_DE_DATOS=<carpeta>`
  (`RutasDeLaAplicacion.VariableDeCarpetaDeDatos`): todo se guarda ahí. SIN esa variable, una copia de prueba abierta
  cuando el YoshiSQL del usuario está cerrado se convierte en la principal, restaura SUS pestañas y, si se cierra
  normalmente, descarta sus scripts sin guardar. Usarla siempre; si no, respaldar `sesion.json` y nunca cerrar la ventana.
- Con automatización de interfaz (UIA) se pueden pulsar botones y capturar ventanas, pero el editor (AvaloniaEdit)
  no expone su texto: no se puede escribir en él, así que subrayado, fragmentos, orden y filtro siguen sin verse en pantalla.
- PENDIENTE: dividir el editor y plan de ejecución como diagrama.

### Compatibilidad con Linux (2026-11-13)
- Revisado: el código ya separaba lo propio de cada sistema (DPAPI solo en Windows; en Linux, permisos 600/700),
  las mayúsculas de archivos y recursos coinciden (Linux distingue mayúsculas) y `dotnet publish -r linux-x64`
  (normal y de un solo archivo) compila sin errores con SkiaSharp y HarfBuzz nativos incluidos.
- Corregido: `EscritorDeArchivosSeguro` crea el temporal ya con permisos 600 (`FileStreamOptions.UnixCreateMode`);
  antes había un instante en que la clave y las contraseñas quedaban legibles por otros usuarios.
- Corregido: la marca de "YoshiSQL abierto" guarda número de proceso y hora de arranque (`RepositorioDeSesionJson.CrearMarca`):
  Linux reutiliza los números de proceso y con `dotnet run` todos se llaman "dotnet".
- CI: `.github/workflows/compilar-y-probar.yml` compila y corre las pruebas en Ubuntu y Windows en cada push.
  Es la única forma de correr las pruebas en Linux desde este equipo (WSL no tiene distribución instalada).
- `empaquetar-linux.sh`: versión tomada de `Directory.Build.props`; dependencias para Ubuntu 22.04–25.04 y Debian 12.
- Sin probar en un Linux real: la ventana, los diálogos de archivos (necesitan xdg-desktop-portal o GTK) y el `.deb`.

### Otros pendientes (según README / ESTRUCTURA)
- Instalador Linux: existe `empaquetar-linux.sh` (.deb) pero falta probarlo en Linux; AppImage no está hecho.
- Probar todo contra servidor real y en Windows 11.
- Otros motores (PostgreSQL, MySQL) como nuevo proveedor en Infraestructura.

## Hoja de ruta (funciones acordadas con el usuario, 2026-10-05)

Leyenda: ⭐ muy útil · 🟢 fácil · 🟡 media · 🔴 difícil

### Prioridad 1 — Ver y editar datos (lo que el usuario pidió primero)
- ✅ HECHO (sin commit) ⭐🟡 Panel SQL en "Editar filas": WHERE y ORDER BY sobre la grilla editable.
  Generador: `GenerarSeleccionDeFilas` con params opcionales `filtroWhere`/`ordenarPor`; UI en `PestanaDeEdicionDeFilas.axaml`.
- ✅ HECHO (sin commit) ⭐🟢 Cantidad de filas configurable en Preferencias (`FilasAlSeleccionar`/`FilasAlEditar`
  en `PreferenciasDelUsuario`; leídas por `ServicioDeGeneracionDeScripts` y `ServicioDeEdicionDeFilas`).
- ✅ HECHO (sin commit) ⭐🟢 Ctrl+0 pone NULL en la celda activa (`GrillaDeEdicion.AlPresionarTecla`).
- ✅ HECHO (sin commit) 🟡 Guardar al cambiar de fila: interruptor opcional en la barra de edición.
  Solo auto-guarda filas MODIFICADAS al salir de ellas (`GuardarFilaModificadaAsync` + `FilaEditableModeloDeVista.ConfirmarGuardado`);
  las nuevas (con identidad) y las eliminadas siguen con el botón, para no tener que re-leer la clave.
- ✅ HECHO (sin commit) 🟡 Filtrar: filtro rápido del lado del cliente en la grilla de edición
  (`DataGridCollectionView` + `FiltroDeFilas`), instantáneo sobre lo cargado. El ORDER BY/WHERE del panel
  ya cubre el ordenar/filtrar en el servidor; no se agregó orden por encabezado (poco fiable con valores mixtos editados).
- 🔴 EVALUADO — no recomendado: Editar filas a través de una vista. La grilla de edición exige llave primaria
  y las columnas de una vista no la reportan, así que quedaría de solo lectura; hacerlo fiable requiere
  detectar la tabla/clave subyacente (frágil). Se deja para después salvo que el usuario insista.

### Prioridad 2 — Funciones rápidas de alto impacto (todas 🟢 salvo nota)
- ✅ HECHO (sin commit) ⭐ Comentar / descomentar líneas con Ctrl+/ (Ctrl+OemQuestion). Avalonia no soporta
  acordes tipo Ctrl+K,Ctrl+C, por eso se usa un solo gesto. Lógica en `EditorSql.AlternarComentario`.
- ✅ HECHO (sin commit) Mayúsculas / minúsculas del texto seleccionado (Ctrl+Shift+U / Ctrl+Shift+L),
  `EditorSql.ConvertirSeleccion`.
- ✅ HECHO (sin commit) ⭐ Filtrar objetos del explorador por nombre (caja arriba del árbol).
  Filtra solo lo ya cargado; `NodoDelArbolModeloDeVista.AplicarFiltro` + `EsVisible`.
- ✅ YA EXISTÍA ⭐ Cambiar de base de datos desde la barra (ComboBox en la barra de herramientas,
  `PestanaDeConsultaModeloDeVista.BaseDeDatosSeleccionada` ejecuta el USE).
- ✅ HECHO (sin commit) ⭐ Color por conexión en la barra de estado. `PerfilDeConexion.Color` (#RRGGBB),
  selector en el diálogo de conexión (`OpcionDeColor`), franja + punto en la barra vía `CadenaHexAPincel`.
- ✅ HECHO (sin commit) ⭐ Resultados en texto plano (Ctrl+T). `ResultadosModeloDeVista.MostrarComoTexto`
  arma una tabla monoespaciada con `ConstruirTexto` (reusa `ValorDeCeldaATexto`); la vista alterna
  grillas/texto con `MostrarGrillaUnica`/`MostrarVariasGrillas`/`MostrarTexto`.
- ✅ HECHO (sin commit) Copiar filas de resultados como INSERT (menú del grid). `ExportadorDeInsert` (formato
  `FormatoDeExportacion.Insert`) con tabla de ejemplo `[dbo].[TablaDestino]` que el usuario reemplaza;
  copia al portapapeles vía `CopiarComoInsertAsync`.
- ✅ HECHO (sin commit) Abrir el valor de una celda en una ventana (XML/JSON formateados): menú
  "Ver valor de la celda..." en el grid → `DialogoDeTexto` + `FormateadorDeValorDeCelda` (usa el Tag de la columna).
- PENDIENTE (fiddly, bajo valor) Sumas / promedios / conteo de selección: el DataGrid de Avalonia selecciona
  por fila, no por celda; mostrar agregados de celdas elegidas es poco fiable. Recomendado dejarlo para después.
- PENDIENTE (fiddly) Marcadores de línea: AvaloniaEdit no trae marcadores; requiere un margen propio. Bajo valor.

### Prioridad 3 — Información de objetos y servidor
- ✅ HECHO (sin commit) ⭐🟡 Propiedades de la tabla (filas, columnas, índices, espacio, fechas): menú "Propiedades"
  → `ObtenerPropiedadesDeTabla.sql` + `PropiedadesDeTabla` + `DescriptorDePropiedades`, se ve en `DialogoDeTexto`.
- ✅ HECHO (sin commit) ⭐🟡 Propiedades de la base de datos (estado, recuperación, compatibilidad, intercalación,
  propietario, tamaño, fecha): menú "Propiedades" → `ObtenerPropiedadesDeBaseDeDatos.sql` + `PropiedadesDeBaseDeDatos`.
- ✅ HECHO (sin commit) ⭐🟡 Ver dependencias (de qué depende el objeto y quién lo usa): menú "Ver dependencias"
  en tablas, vistas, procedimientos y funciones. `ObtenerDependencias.sql` (sys.sql_expression_dependencies)
  + `DependenciaDeObjeto`; se agrupa por relación en `DialogoDeTexto`.
- ✅ HECHO (sin commit) ⭐🟡 Mantenimiento de índices: acción "Ver fragmentación de índices" en la tabla
  (`sys.dm_db_index_physical_stats`, abre y ejecuta) y "Reconstruir (REBUILD)" / "Reorganizar (REORGANIZE)"
  en cada índice. `GenerarConsultaDeFragmentacion` + `GenerarMantenimientoDeIndice`.
- ✅ HECHO (sin commit) 🟢 Renombrar objeto (sp_rename): acción "Renombrar..." en tablas, vistas, procedimientos
  y funciones. Estrena `DialogoDeEntrada` + `IServicioDeDialogos.PedirTextoAsync` (reutilizable). El script
  `GenerarRenombrado` se abre y ejecuta; el usuario refresca el árbol con "Actualizar".
- ✅ HECHO (sin commit) 🟢 Script como SELECT / INSERT / UPDATE / DELETE desde el menú del explorador
  (tablas: los 4; vistas: SELECT). `IGeneradorDeScripts.GenerarInstruccionDml` + `TipoDeScriptDml`,
  servicio `GenerarInstruccionDmlAsync`, acciones en `FabricaDeNodos`. EXEC de procedimientos queda PENDIENTE
  (necesita leer los parámetros del procedimiento del catálogo).
- ✅ HECHO (sin commit) 🟢 Ejecutar procedimiento (EXEC con parámetros): acción "Generar EXEC..." en los nodos de
  procedimiento. Lee `sys.parameters` (`ListarParametros.sql` + `ParametroDeProcedimiento`), genera la plantilla
  EXEC con placeholders para la entrada y DECLARE/OUTPUT/SELECT para la salida, y la abre en el editor para
  completarla y ejecutarla. `GenerarEjecucionDeProcedimiento` + `GenerarEjecucionDeProcedimientoAsync`.
- 🟡 Más carpetas en el explorador: ✅ HECHO (sin commit) Disparadores (triggers) bajo cada tabla
  (`ListarDisparadores.sql` + `Disparador` + `TipoDeNodo.Disparador`). PENDIENTE: restricciones (CHECK/DEFAULT),
  sinónimos, tipos de usuario, esquemas, secuencias.
- ✅ HECHO (sin commit) 🟢 Visor del log del servidor: acción "Ver log de errores del servidor" en el nodo del
  servidor que abre y ejecuta `EXEC sys.sp_readerrorlog;`.
- ✅ HECHO (sin commit) 🟢 SET STATISTICS IO/TIME: toggle "Incluir estadísticas (IO y tiempo)" en el menú Consulta.
  `ISesionDeConsulta.EjecutarConEstadisticasAsync` envuelve con SET STATISTICS IO, TIME ON/OFF; salen en Mensajes.
- 🟡 Bloqueos en árbol (quién bloquea a quién) como mejora del monitor.

### Prioridad 4 — Funciones que DIFERENCIAN de SSMS (el valor real de YoshiSQL)
El usuario destacó estas como "espectaculares". No son copiar a SSMS; son el motivo de existir de la app.
- ✅ HECHO (sin commit) ⭐🔴 **Exportar toda la base a un .sql**: botón derecho en la base → "Exportar todo a
  script...". Un `DialogoDeExportacion` deja ELEGIR qué incluir (estructura de tablas, datos, vistas,
  procedimientos, funciones). Genera CREATE TABLE + INSERT (con SET IDENTITY_INSERT) + llaves foráneas, y el
  CREATE de funciones/vistas/procedimientos desde su definición (orden: tablas→datos→FKs→funciones→vistas→procs).
  `OpcionesDeExportacion` + `ServicioDeExportacionDeBaseDeDatos` (Aplicacion); generador:
  `GenerarCreacionDeTablaSinEnvolver`, `GenerarInsertDeFilas`, `GenerarLlaveForanea`, `GenerarUso`, `GenerarSeleccionCompleta`.
  Objetos cifrados/sin permiso se omiten con un comentario. Desde 2026-11-09 incluye también tipos tabla, secuencias,
  columnas calculadas, índices, UNIQUE, DEFAULT, CHECK, disparadores y cascadas (ver "Revisión contra SQL Server real").
  PENDIENTE (idea): exportar una sola tabla desde su menú.
- ⭐🔴 **Comparar dos bases (esquema + datos)**: muestra tablas/columnas nuevas, distintas o borradas y
  genera el script de sincronización. SSMS no lo trae gratis → función estrella diferenciadora.
- ✅ HECHO (sin commit) ⭐🟡 **Panel "salud de la base"** (v1): pestaña (botón derecho en la base → "Salud de la
  base de datos") con resumen (tablas, filas, tamaño, espacio usado), tablas más pesadas e índices más fragmentados.
  `SaludResumen.sql`/`SaludTablasPesadas.sql`/`SaludIndicesFragmentados.sql` + records en `Dominio.Salud` +
  `IExploradorDeEsquema.ObtenerSaludAsync` + `PestanaDeSaludModeloDeVista`/`PestanaDeSalud`. PENDIENTE (v2):
  índices que faltan (de los planes) y consultas más lentas.
- ✅ HECHO (sin commit) 🟢 **Favoritos / fragmentos de consultas**: pestaña "Favoritos" en la barra lateral.
  "Editar → Guardar como favorito..." guarda el texto de la consulta con un nombre; doble clic lo abre en una
  consulta nueva. `ConsultaFavorita` + `IRepositorioDeFavoritos`/`RepositorioDeFavoritosJson` (favoritos.json en
  config) + `ServicioDeFavoritos` + `FavoritosModeloDeVista`/`PanelDeFavoritos` + `IAccionesDeFavoritos`.

### Imagen de la app (branding) — HECHO
- Logo: imagen PNG generada en `Recursos/logo.png` (tile verde con cilindro de BD + rayo ámbar). El control
  `Controles/LogoDeYoshiSql` ahora solo muestra ese PNG; se usa en el splash y en "Acerca de".
- Ícono de la ventana: `VentanaPrincipal.axaml` usa `Icon="/Recursos/logo.png"`.
- Ícono del ejecutable: `Recursos/yoshisql.ico` (multi-tamaño) vía `<ApplicationIcon>` en el csproj.
- Splash: `Vistas/Comunes/PantallaDeBienvenida` (sin bordes, Topmost, ~1.6 s) con el logo, versión y barra.
- Las imágenes se generaron con una herramienta Avalonia+Skia headless (en scratchpad, no versionada) que
  dibuja el logo y lo exporta a PNG/ICO. Para rehacerlas, re-renderizar con esa técnica (`RenderTargetBitmap.Save`).

### Prioridad 5 — Grandes / a futuro
- 🔴 Importar / exportar datos desde CSV o Excel a una tabla.
- 🟢 Registered Servers / grupos: organizar conexiones en carpetas (Producción, Desarrollo).
- 🟡 Snippets de T-SQL (CREATE PROCEDURE, TRY/CATCH, cursores) con Tab.
- 🟡 Subrayado de errores de sintaxis en vivo (ScriptDom ya está en el proyecto).
- 🟡 Ir a la definición (F12), plegar código, selección en columna.
- 🟡 Detach / attach de archivos .mdf.
- 🔴 Query Store, SQL Server Agent, XEvents, comparar planes de ejecución.

### Sobre bases cifradas / .bak protegidos (contexto del usuario)
No se rompe el cifrado de SQL Server sin las claves, y hacerlo suele violar el contrato con el proveedor:
no es una vía a implementar. La vía legítima y donde YoshiSQL aporta es **extraer y portar datos** que el
usuario sí puede leer (exportar tablas, generar el .sql de la base). Por eso "Exportar toda la base a .sql"
es prioridad en P4.

## Rediseño UX/UI (2026-10-08) — en progreso
- Barra de herramientas con iconos (`Estilos/Iconos.axaml` = StreamGeometry 16px; estilos `Path.IconoBarra`/`Path.IconoEjecutar`).
- Pestañas laterales con icono (acento en la seleccionada) y logo en la pantalla inicial.
- ⭐ Paleta de comandos (Ctrl+Shift+P / Ctrl+P): `DialogoDePaleta` + `PaletaDeComandosModeloDeVista` + `ComandoDePaleta`;
  la ventana principal arma la lista en `ConstruirComandosDePaleta` (globales + de la consulta activa). Resuelve
  la baja descubribilidad (muchas acciones solo estaban en el clic derecho).
- PENDIENTE de pulido: iconos en menús contextuales, estados hover/selección más marcados en árbol/listas.

## Bug corregido (2026-10-08): tipos CLR (geography/geometry/hierarchyid/UDT)
Leer tablas con columnas de tipos CLR (ej. AdventureWorks: `SpatialLocation` geography, `OrganizationNode`
hierarchyid) lanzaba `FileNotFoundException` de `Microsoft.SqlServer.Types` (ensamblado no instalado), tanto al
exportar como al "Seleccionar filas". `SesionDeConsultaSqlServer.LeerValorDeCelda` ahora lee cada celda con
`GetValue`; si falla por no cargar ese ensamblado, cae a `GetSqlBytes` (bytes serializados) → se ven como 0x... en
la grilla y en el INSERT del export. No necesita la dependencia Microsoft.SqlServer.Types.

## Calidad / bugs revisados (2026-10-07)
- Corregido antes: desconectar cerraba el nodo pero dejaba viva la conexión de la pestaña (ya cierra pestañas y
  libera conexión); conectar al mismo servidor duplicaba el nodo (ahora se deduplica por servidor+usuario).
- Panel de salud: visibilidad enlazada a `.Count` (int) → se cambió a convertidor `HayElementos`; ahora deduplica
  la pestaña y muestra el mensaje de error en el cuerpo si falla.
- Pruebas nuevas: `ServicioDeFavoritosPruebas` (5) y literales de `GenerarInsertDeFilas` (fechas, GUID, binarios,
  texto con comillas). Total: 151 (Dominio 25, Aplicacion 37, Infraestructura 89, 1 omitida en Windows).
- Nota: al reabrir, las pestañas se restauran pero YoshiSQL NUNCA se conecta solo: abre la ventana "Conectar"
  con el servidor elegido y el usuario confirma (B-57, por seguridad). "Conectar"
  siempre está en la barra de herramientas y en el menú; el botón grande del centro solo aparece sin pestañas.

## Notas para trabajar
- **Bugs: `BUGS.md` en la raíz es el registro oficial.** Todo bug nuevo encontrado (en revisiones o por el usuario)
  se agrega a "Por arreglar" con el siguiente ID libre (`B-NN`, no se reutilizan). Al corregirlo se mueve a
  "Arreglados" con fecha y cómo se resolvió. Prioridad: primero lo que inutiliza el sistema (🔴).
- Al terminar una función, actualizar la sección "ESTADO ACTUAL" de `ESTRUCTURA_YOSHISQL.txt`, el README y este archivo.
- Agregar pruebas en la capa correspondiente y mantener `dotnet build` sin advertencias.
