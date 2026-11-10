# Registro de bugs de YoshiSQL

Lista viva de errores encontrados en revisiones y en el uso real. Reglas:

- Cada bug tiene un **ID fijo** (`B-NN`) que no se reutiliza, para poder nombrarlo en commits y conversaciones.
- Al arreglarlo se **mueve** de "Por arreglar" a "Arreglados", con la fecha y una línea de cómo se resolvió.
- Si al revisar código aparece un bug nuevo, se agrega al final de "Por arreglar" con el siguiente ID libre.
- Gravedad: 🔴 inutiliza el sistema o daña datos · 🟠 importante · 🟡 menor.
- "Por verificar" = el arreglo o el bug se dedujo leyendo código; falta comprobarlo contra un SQL Server real.

Siguiente ID libre: **B-65**

---

## Por arreglar

### 🔴 Críticos

_Ninguno por ahora._

### 🟠 Importantes

_Ninguno por ahora._

### 🟡 Menores

| ID | Bug | Dónde | Detalle |
|---|---|---|---|
| B-63 | Exportar no incluye algunos objetos poco comunes | `ServicioDeExportacionDeBaseDeDatos.cs` | Usuarios y permisos, índices columnstore/XML/espaciales/de texto completo, índices sobre vistas, colecciones de esquemas XML, disparadores de base de datos (DDL), objetos CLR, propiedades extendidas, intercalación propia de una columna, y restricciones dentro de un tipo tabla (salvo su llave primaria). Los sinónimos ya se exportan (2026-11-10). El script lo avisa en su encabezado. En AdventureWorks2022 solo fallan los 3 objetos que usan esquemas XML o texto completo. |

---

## Arreglados

| ID | Fecha | Bug | Cómo se resolvió |
|---|---|---|---|
| B-40 | 2026-10-25 | Un error léxico en un procedimiento juntaba todo el script en un lote y fallaban todos los CREATE PROCEDURE | `DivisorDeLotesTSql` separa línea a línea como SSMS/sqlcmd (GO solo en su línea, fuera de textos, `[identificadores]` y comentarios anidados). |
| B-41 | 2026-10-25 | Scripts con saltos de línea solo `\r` lanzaban `ArgumentOutOfRangeException` | El divisor acepta `\r\n`, `\n` y `\r`. |
| B-42 | 2026-10-25 | `dotnet build` fallaba por vulnerabilidades de `System.Security.Cryptography.Pkcs` 9.0.18 | Referencia directa a 10.0.13 en Infraestructura. |
| B-43 | 2026-10-25 | Al reconectar, la pestaña volvía a la base inicial y no a la que se usaba (el script corría en otra base) | `SesionDeConsultaSqlServer._baseDeDatosEnUso`; cambiar de base mientras se ejecuta da un mensaje claro. |
| B-44 | 2026-10-25 | Celda editada y devuelta a su valor generaba `UPDATE ... SET  WHERE` y no se guardaba nada | Se omiten las filas sin cambios reales (`ObtenerPosicionesModificadas`). |
| B-45 | 2026-10-25 | `10,5` en una columna decimal se guardaba como 105; fechas `dd/MM` se leían como `MM/dd` | `ConvertidorDeValoresDeEdicion` rechaza esos formatos con una sugerencia clara. |
| B-46 | 2026-10-25 | Exportar/copiar como INSERT escribía fechas que un login en español lee como año-día-mes | Fechas en ISO 8601 con `T`. Limitación: 3 decimales (datetime2(7) pierde sub-ms). |
| B-47 | 2026-10-25 | `conexiones.json` dañado impedía ver y guardar conexiones | Se copia a `.danado` y se empieza vacío. |
| B-48 | 2026-10-25 | Un fallo al liberar una pestaña impedía cerrar la aplicación | Se registra y se cierra igual. |
| B-49 | 2026-10-25 | Exportar incluía columnas rowversion y calculadas en los INSERT (todos fallaban) | Se omiten en `GenerarInsertDeFilas`. |
| B-50 | 2026-10-25 | La app se congelaba tras ejecutar scripts grandes (8.000 SELECT = 77 s) | Regex lineal en `DetectorDeCambiosDeEsquema` + tiempo máximo; 50.000 SELECT < 1 s. |
| B-51 | 2026-10-25 | Cada ejecución armaba una tabla de texto con todas las filas; miles de conjuntos creaban miles de grillas | Texto solo en modo texto y hasta 5.000 filas; máximo 50 grillas con aviso. |
| B-52 | 2026-10-25 | Varios bloqueos de uso diario | Conexión no falla si no se puede guardar la contraseña, y el almacén de credenciales se repara solo (credenciales dañadas o clave DPAPI ilegible). Edición de filas: conteo con `@@ROWCOUNT` (disparadores), fechas con su tipo (`ParametroSql.TipoEnElServidor`), columnas calculadas de solo lectura. Autocompletado solo en el lote del cursor. Compresión de respaldo según edición (Express/Web no). Restaurar libera el pool. Salud con 600 s de límite. **Por verificar:** `@@ROWCOUNT` con disparadores INSTEAD OF. |
| B-10 | 2026-11-10 | Diseñador: no se podía borrar una columna con DEFAULT ni agregar una columna NOT NULL a una tabla con datos | Antes del `DROP COLUMN` se busca y quita su restricción DEFAULT; una columna NOT NULL nueva se agrega con un valor inicial temporal (0, texto vacío, 1900-01-01...) para las filas existentes y luego se quita ese valor predeterminado. **Verificado con SQL Server real.** |
| B-14 | 2026-11-10 | Exportar cargaba toda la base en memoria, sin avance ni cancelar | `ServicioDeExportacionDeBaseDeDatos.ExportarAsync` escribe directo al archivo; las filas se leen por tramos (`IEjecutorDeConsultas.LeerFilasAsync`). Nueva ventana `DialogoDeProgreso` (`IServicioDeDialogos.EjecutarConProgresoAsync`) con avance y Cancelar; si se cancela, el archivo a medias se borra. **Verificado con SQL Server real.** La ventana, por verificar. |
| B-15 | 2026-11-10 | Editar filas: se sobrescribían los cambios de otra persona | El `UPDATE` exige, además de la llave, que las columnas que se cambian sigan con el valor que el usuario vio (`CrearCondicionDeValoresOriginales`); si no, avisa y no guarda. Cambios de otra persona en otras columnas no estorban. **Verificado con SQL Server real.** |
| B-16 | 2026-11-10 | Recuperación: dos pestañas con el mismo nombre de archivo se pisaban | `ElegirRutaLibre`: la segunda se guarda como `nombre (2).sql`. Por verificar en la aplicación. |
| B-17 | 2026-11-10 | Dos ventanas de YoshiSQL se pisaban la sesión guardada | La segunda ventana se reconoce (`IRepositorioDeSesion.EsInstanciaSecundaria`), empieza vacía y no guarda sesión. También se comprueba el nombre del proceso de la marca, porque el sistema reutiliza los números de proceso. Por verificar en la aplicación. |
| B-19 | 2026-11-10 | Consultas de catálogo con 30 s de límite | 120 s para explorador y autocompletado (`SegundosParaConsultasDeCatalogo`). |
| B-20 | 2026-11-10 | Cambiar rápido de perfil en "Conectar" podía dejar la contraseña de otro | La contraseña se vacía al cambiar de perfil y solo se aplica si el perfil sigue siendo el mismo cuando llega. Por verificar en la aplicación. |
| B-21 | 2026-11-10 | Abrir del historial sin su servidor conectado usaba la base del historial en otro servidor | Si el servidor original no está conectado, se abre en el actual sin forzar la base. Por verificar en la aplicación. |
| B-22 | 2026-11-10 | Formatear con `QUOTED_IDENTIFIER OFF` cambiaba textos por identificadores | **No se reproduce:** probado con el formateador real, `"hola"` se conserva (o pasa a `'hola'`, que es equivalente con esa opción apagada). Se cierra sin cambios. |
| B-23 | 2026-11-10 | El texto "primeras N filas" del menú no se actualizaba al cambiar preferencias | `AccionDelNodo` admite un texto calculado, que se lee cada vez que se abre el menú. Por verificar en la aplicación. |
| B-24 | 2026-11-10 | Arrastrar una tabla del diagrama quedaba pegada si se perdía el mouse | Se maneja `PointerCaptureLost`. Por verificar en la aplicación. |
| B-25 | 2026-11-10 | Reconectar al mismo servidor conservaba credenciales viejas en el árbol | Si la contraseña cambió, el nodo se reemplaza en su mismo lugar. Por verificar en la aplicación. |
| B-26 | 2026-11-10 | Los errores inesperados mostraban un mensaje genérico | El mensaje incluye la causa (`Causa: ...`) además del código de registro. |
| B-27 | 2026-11-10 | "Ver dependencias" fallaba si `OBJECT_NAME` devolvía NULL | Se omiten los objetos cuyo nombre no se puede ver. |
| B-28 | 2026-11-10 | Guardados seguidos de favoritos podían chocar | Exclusión mutua al guardar, como en el historial. |
| B-29 | 2026-11-10 | CSV: celdas que empiezan con `=` se ejecutaban como fórmula en Excel | A los textos que empiezan con `=`, `+`, `-`, `@` se les antepone un apóstrofo; los números negativos no se tocan. |
| B-30 | 2026-11-10 | Cerrar pestaña con consulta en curso o transacción abierta no avisaba | `ISesionDeConsulta.TieneTransaccionAbiertaAsync` + aviso con confirmación al cerrar la pestaña o la aplicación. **Verificado con SQL Server real.** El aviso, por verificar. |
| B-31 | 2026-11-10 | El avance de respaldo/restauración solo se veía al final y no se podía cancelar | `ISesionDeConsulta.MensajeRecibido` entrega cada mensaje al llegar; las dos ventanas muestran el porcentaje en vivo y tienen botón de cancelar. **Verificado con SQL Server real.** Las ventanas, por verificar. |
| B-32 | 2026-11-10 | No se podía desmarcar una fila marcada para eliminar | "Eliminar fila" sobre una fila ya marcada la desmarca y conserva lo editado. Por verificar en la aplicación. |
| B-33 | 2026-11-10 | "Generar EXEC": tipos de parámetros sin esquema | Resuelto junto con B-64: el catálogo devuelve el tipo con su esquema cuando no es `dbo`. |
| B-34 | 2026-11-10 | Restaurar renombraba los contenedores FILESTREAM como `.ndf` | `ArchivoDelRespaldo.EsCarpeta` (tipo `S`): se ubican como carpeta, sin extensión. **Por verificar con una base FILESTREAM.** |
| B-35 | 2026-11-10 | Expandir una base sin acceso daba error en el árbol | `ListarBasesDeDatos.sql` usa `HAS_DBACCESS`: se muestra como no disponible. |
| B-36 | 2026-11-10 | Restaurar: ventana entre SINGLE_USER y RESTORE | Ambas instrucciones van en el mismo lote. **Verificado con SQL Server real.** |
| B-37 | 2026-11-10 | El script del diseñador dejaba `SET XACT_ABORT ON` en la pestaña | Termina con `SET XACT_ABORT OFF` en un lote aparte, que se ejecuta aunque el anterior falle. **Verificado con SQL Server real.** |
| B-38 | 2026-11-10 | El historial guardaba contraseñas en texto plano | `HistorialDeConsultas.OcultarContrasenas`: `PASSWORD = '...'` y `SECRET = '...'` se guardan como `'***'`. |
| B-39 | 2026-11-10 | Ejecutar con miles de mensajes era lento | `ResultadosModeloDeVista.Mensajes` se reemplaza de una vez en lugar de agregar uno por uno. Por verificar en la aplicación. |
| B-62 | 2026-11-10 | Exportar recreaba toda `IDENTITY` como `IDENTITY(1,1)` | `ListarIdentidades.sql` + `IdentidadDeColumna`: se conservan semilla e incremento. **Verificado con SQL Server real.** |
| B-64 | 2026-11-10 | Un tipo alias fuera de `dbo` se escribía sin su esquema | Las consultas de columnas y parámetros devuelven `esquema.Tipo`; `TipoDeDato` lo acepta. **Verificado con SQL Server real.** |
| B-54 | 2026-11-09 | **Exportar no creaba los tipos tabla**: los procedimientos con parámetros de tipo tabla fallaban al ejecutar el script ("no se encuentra el tipo de datos", "debe declarar la variable de tabla") | Detectado al exportar y volver a ejecutar las bases reales del equipo (PuntoVentaDB: 12 procedimientos fallaban). Nuevo `ListarColumnasDeTiposDeTabla.sql` + `TipoDeTabla` + `GenerarCreacionDeTipoDeTabla` (condicional, con su llave primaria). Van antes que tablas y procedimientos. |
| B-11 | 2026-11-09 | **Exportar perdía DEFAULT, CHECK, UNIQUE, índices, disparadores y las cascadas de las llaves foráneas**: la copia se creaba "sin errores" pero no funcionaba (inserciones que fallan por columnas NOT NULL sin valor predeterminado, llaves foráneas que apuntan a un UNIQUE inexistente) | `IExploradorDeEsquema.ObtenerDetallesDelEsquemaAsync` lee todo de una vez (`DetallesDelEsquema`, 8 consultas de catálogo nuevas); el generador crea cada pieza y el servicio las ordena: índices y UNIQUE antes de las llaves foráneas, DEFAULT y CHECK después de las funciones, disparadores al final. Se conservan nombres, `INCLUDE`, filtros, orden descendente, `ON DELETE/UPDATE` y el estado deshabilitado. **Verificado:** las 8 bases propias del servidor local se exportan y recrean con 0 errores, los mismos objetos y los mismos datos (suma de comprobación por tabla). Queda aparte la semilla de `IDENTITY` (B-62). |
| B-12 | 2026-11-09 | Exportar creaba las columnas calculadas como columnas normales vacías | `ListarColumnasCalculadas.sql` + `ColumnaCalculada`: el CREATE TABLE lleva la fórmula (`AS ... PERSISTED`). |
| B-13 | 2026-11-09 | Exportar creaba vistas y funciones en orden alfabético y fallaban las que dependían de otras | `OrdenadorPorDependencias` (con `ListarDependenciasEntreModulos.sql`) las pone en el orden en que se necesitan; funciones y vistas van en una sola sección. |
| B-58 | 2026-11-09 | Exportar no creaba las secuencias (`CREATE SEQUENCE`) | `ListarSecuencias.sql` + `Secuencia` + `GenerarCreacionDeSecuencia`: continúa desde el siguiente valor sin repetir números; condicional. |
| B-59 | 2026-11-09 | Exportar fallaba en tablas con una columna calculada que llama a una función (la función se creaba después de la tabla) | Las funciones que usan las tablas (y las que ellas usan) se crean antes que las tablas. Encontrado con AdventureWorks (`Sales.Customer`). |
| B-60 | 2026-11-09 | Exportar ponía todos los INSERT de una tabla en un solo lote y cada fila devolvía su mensaje | Lotes de 1.000 filas (`FilasPorLote`) y `SET NOCOUNT ON` en la sección de datos (se restaura al final). |
| B-18 | 2026-11-09 | **Abrir un .sql guardado en ANSI (lo habitual en SSMS y Windows) rompía tildes y eñes**: columnas como `[Descripción]` o textos `'Año'` llegaban como `�` y el script fallaba o guardaba datos corruptos | `DetectorDeCodificacion`: usa la marca inicial si existe; si no, UTF-8 cuando es válido, UTF-16 sin marca por sus ceros, y si no Windows-1252. Funciona igual en Linux. |
| B-61 | 2026-11-09 | Un error dentro de un procedimiento ejecutado con EXEC señalaba una línea cualquiera del editor y no decía en qué procedimiento ocurrió | Los mensajes ahora dicen "Procedimiento X, Línea N" como SSMS; la línea solo se traduce al editor cuando el lote es el que crea ese objeto. |
| B-57 | 2026-10-25 | **Seguridad:** al abrir YoshiSQL se reconectaba solo (con la contraseña guardada) a los servidores de las pestañas anteriores; el servidor aparecía conectado y se podía, por ejemplo, eliminar bases sin haber pulsado "Conectar" | Reportado por el usuario. Se quitó `ReconectarSinPreguntarAsync`: para restaurar pestañas se abre la ventana "Conectar" con el servidor elegido y solo se conecta si el usuario lo confirma. Si cancela, nada queda conectado y los scripts sin guardar van a la carpeta de recuperados. |
| B-05 | 2026-10-25 | Al cambiar de pestaña se perdían cursor, selección y desplazamiento; riesgo de usar la selección de otra pestaña | `EditorSql` recuerda el estado por documento (`ConditionalWeakTable`) y lo restaura; `RangoDeTexto.Documento` + validación en `ObtenerRangoAProcesar`: solo se ejecuta una selección del propio documento. **Por verificar en la app.** |
| B-06 | 2026-10-25 | Restaurar un .bak con varios respaldos tomaba el primero (el más viejo) | Se lee todo `RESTORE HEADERONLY`, se elige el respaldo completo más reciente y se usa `WITH FILE = n` (también en FILELISTONLY). La ventana avisa cuántos respaldos tiene el archivo. |
| B-07 | 2026-10-25 | Excel redondeaba bigint/decimales largos y no controlaba el límite de filas | Más de 15 dígitos se escriben como texto; más de 1.048.575 filas da un mensaje claro (usar CSV). Todas las exportaciones de resultados corren fuera del hilo de la interfaz. |
| B-08 | 2026-10-25 | El diseñador perdía la precisión de datetime2/time/datetimeoffset | El catálogo lee `scale` para esos tipos (columnas, parámetros, tipos de usuario); `Interpretar` acepta `datetime2(0)`. |
| B-09 | 2026-10-25 | Tipos de usuario con mayúsculas se veían modificados (ALTER COLUMN innecesarios) | `TipoDeDato` compara sin distinguir mayúsculas; los tipos del sistema se normalizan a minúsculas y los de usuario conservan las suyas. |
| B-01 | 2026-10-25 | Exportar todo no creaba esquemas ni tipos de usuario (fallaba en AdventureWorks) | El script empieza con `CREATE SCHEMA` (vía `EXEC`, condicional) y `CREATE TYPE ... FROM` (condicional). Nuevas consultas `ListarEsquemasDeUsuario.sql` y `ListarTiposDeUsuario.sql`. Pendiente aparte: tipos tabla (B-54). |
| B-02 | 2026-10-25 | Exportar: una tabla salía con 0 filas sin avisar si su SELECT fallaba | Se escribe `-- ERROR:` en el script y se avisa al terminar (`ResultadoDeExportacion.Advertencias`), igual que los objetos cifrados. |
| B-03 | 2026-10-25 | Exportar: nombres de PK repetidos entre esquemas | Se usa la PK real del catálogo (ver B-56). |
| B-04 | 2026-10-25 | Exportar y guardar .sql sin BOM: tildes corruptas en sqlcmd/SSMS | El export se guarda en UTF-8 con BOM; los scripts guardados llevan BOM solo si tienen caracteres no ASCII. |
| B-56 | 2026-10-25 | CREATE TABLE inventaba el nombre de la PK y perdía el orden de sus columnas y si era agrupada | `GenerarCreacionDeTablaSinEnvolver` y "Generar script CREATE TABLE" reciben el `Indice` de la PK (nombre, columnas por `key_ordinal`, CLUSTERED/NONCLUSTERED). |
| B-53 | 2026-10-25 | Faltaba eliminar base de datos; con conexiones abiertas fallaba o quedaba en SINGLE_USER | "Eliminar..." en el menú: libera el pool, mueve pestañas a master, SINGLE_USER en TRY/CATCH que vuelve a MULTI_USER si el DROP falla, confirmación escribiendo el nombre. |

---

## Verificado contra un SQL Server real (2026-11-09, SQL Server 2022 local)

- **Dividir por GO:** los 836 procedimientos, funciones, vistas y disparadores reales de las bases del equipo, unidos con `GO` en un solo script (con saltos `\r\n` y con `\n`), se separan uno por lote y pasan el análisis del servidor sin errores.
- **Ejecutar:** errores de sintaxis, errores en tiempo de ejecución, `RAISERROR`, `THROW`, `GO n`, `USE`, `XACT_ABORT`, tipos CLR: mensajes y números de línea correctos.
- **Editar filas:** tabla con disparador `AFTER` sin `NOCOUNT`, disparador `INSTEAD OF` que devuelve filas, columna calculada, `rowversion` y llave `datetime2(7)`: guarda bien y cuenta bien las filas.
- **Respaldo, restauración y eliminar base:** con otra sesión abierta dentro de la base; sin "cerrar conexiones" da un mensaje claro y con la opción funciona y la base queda en `MULTI_USER`.
- **2026-11-10:** diseñador (quitar columna con DEFAULT, agregar NOT NULL con datos), concurrencia al editar filas, transacción abierta, mensajes en vivo, lectura por tramos y su cancelación, cancelar un respaldo, y exportar con tipos de otro esquema, `IDENTITY(1000,5)` y sinónimos.
