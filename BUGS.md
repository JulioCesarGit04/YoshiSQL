# Registro de bugs de YoshiSQL

Lista viva de errores encontrados en revisiones y en el uso real. Reglas:

- Cada bug tiene un **ID fijo** (`B-NN`) que no se reutiliza, para poder nombrarlo en commits y conversaciones.
- Al arreglarlo se **mueve** de "Por arreglar" a "Arreglados", con la fecha y una línea de cómo se resolvió.
- Si al revisar código aparece un bug nuevo, se agrega al final de "Por arreglar" con el siguiente ID libre.
- Gravedad: 🔴 inutiliza el sistema o daña datos · 🟠 importante · 🟡 menor.
- "Por verificar" = el arreglo o el bug se dedujo leyendo código; falta comprobarlo contra un SQL Server real.

Siguiente ID libre: **B-58**

---

## Por arreglar

### 🔴 Críticos

_Ninguno por ahora._

### 🟠 Importantes

| ID | Bug | Dónde | Detalle |
|---|---|---|---|
| B-10 | Diseñador: no se puede borrar una columna con DEFAULT ni agregar NOT NULL a una tabla con datos | `GeneradorDeScriptsSqlServer.cs` (`CrearInstruccionesDeModificacion`) | Falta quitar la restricción DEFAULT antes del DROP y soporte de valores DEFAULT. |
| B-11 | Exportar no lleva restricciones ni índices | Generador + `ListarLlavesForaneas.sql` | Se pierden `ON DELETE/UPDATE CASCADE`, `DEFAULT`, `CHECK`, `UNIQUE`, índices (salvo la PK), disparadores y la semilla/incremento de `IDENTITY`. La base recreada no es equivalente. (La PK ya conserva nombre, orden y tipo: B-56.) |
| B-12 | Exportar no maneja columnas calculadas en el CREATE TABLE | `GeneradorDeScriptsSqlServer.cs` (`DefinirColumna`) | Se crean como columnas normales (quedan en NULL). Desde B-49 ya no rompen los INSERT. |
| B-13 | Exportar crea vistas, funciones y procedimientos en orden alfabético | `ServicioDeExportacionDeBaseDeDatos.cs:75-91` | Los objetos que dependen de otros que van después fallan. Falta ordenar por dependencias. |
| B-14 | Exportar carga toda la base en memoria, sin progreso ni cancelar | `ServicioDeExportacionDeBaseDeDatos.cs` | Bases grandes: mucha memoria y la app parece colgada. |
| B-15 | Editar filas: se sobrescriben cambios de otra persona | `GeneradorDeScriptsSqlServer.cs` (`CrearCondicionPorLlave`) | El WHERE solo usa la PK; si otro modificó la fila mientras tanto, gana el último (sin concurrencia optimista). |
| B-16 | Recuperación tras cierre inesperado: archivos con el mismo nombre se pisan | `Infraestructura/Persistencia/RepositorioDeSesionJson.cs:92` | Dos pestañas `consulta.sql` de carpetas distintas → se pierde una. |
| B-17 | Dos ventanas de YoshiSQL abiertas se pisan la sesión guardada | `RepositorioDeSesionJson.cs` | El autoguardado de cada una sobrescribe el `sesion.json` de la otra. |
| B-18 | Abrir un .sql en ANSI (Windows-1252) muestra mal las tildes | `RepositorioDeScriptsEnDisco.cs:11` | Y al guardarlo se corrompen. Falta detectar la codificación. |
| B-19 | Consultas de catálogo con 30 s de límite | `Infraestructura/SqlServer/ExploradorDeEsquemaSqlServer.cs` (`LeerFilasAsync`) | La Salud ya se corrigió (B-52); explorador y autocompletado en bases muy grandes pueden seguir agotando el tiempo. |

### 🟡 Menores

| ID | Bug | Dónde | Detalle |
|---|---|---|---|
| B-20 | Cambiar rápido de perfil en "Conectar" puede dejar la contraseña de otro | `Escritorio/ModelosDeVista/Conexiones/DialogoDeConexionModeloDeVista.cs` (`LlenarConPerfilAsync`) | Carrera entre lecturas asíncronas de contraseñas. |
| B-21 | Abrir del historial sin su servidor conectado lo abre en otro servidor | `VentanaPrincipalModeloDeVista.cs` (`AbrirConsultaDelHistorialAsync`) | Usa el servidor actual con la base del historial. |
| B-22 | Formatear con `QUOTED_IDENTIFIER OFF` cambia textos por identificadores | `Infraestructura/SqlServer/FormateadorDeSqlTSql.cs` | `"hola"` pasa a `[hola]`. |
| B-23 | El texto "primeras N filas" del menú no se actualiza al cambiar preferencias | `Escritorio/ModelosDeVista/Explorador/FabricaDeNodos.cs` | La acción sí usa el valor nuevo. |
| B-24 | Arrastrar una tabla del diagrama queda pegada si se pierde el mouse | `Escritorio/Vistas/Diagramas/PestanaDeDiagrama.axaml.cs` | Falta manejar `PointerCaptureLost`. |
| B-25 | Reconectar al mismo servidor conserva credenciales viejas en el árbol | `Escritorio/ModelosDeVista/Explorador/ExploradorModeloDeVista.cs` (`AgregarServidor`) | Si cambió la contraseña, el explorador sigue fallando hasta desconectar. |
| B-26 | Errores inesperados muestran un mensaje genérico | `Aplicacion/Errores/RegistroDeErrores.cs` | Oculta la causa real (ej. "acceso denegado" al guardar un archivo). |
| B-27 | "Ver dependencias" falla si `OBJECT_NAME` devuelve NULL | `ExploradorDeEsquemaSqlServer.cs` (`ObtenerDependenciasAsync`) | Pasa sin permiso VIEW DEFINITION. |
| B-28 | Guardados seguidos de favoritos pueden chocar | `Aplicacion/Consultas/ServicioDeFavoritos.cs` | Sin exclusión mutua; usan el mismo `.tmp`. |
| B-29 | CSV con celdas que empiezan con `=` se ejecutan como fórmula en Excel | `Infraestructura/Exportacion/ExportadorCsv.cs` | Inyección de fórmulas. |
| B-30 | Cerrar pestaña con consulta en curso o transacción abierta no avisa | `VentanaPrincipalModeloDeVista.cs` (`CerrarPestanaAsync`) | La transacción se deshace en silencio. |
| B-31 | Avance de respaldo/restauración solo al final y sin cancelar | `Infraestructura/SqlServer/SesionDeConsultaSqlServer.cs` | Los mensajes de `STATS` llegan juntos al terminar. |
| B-32 | No se puede desmarcar una fila marcada para eliminar | `Escritorio/ModelosDeVista/EdicionDeFilas/PestanaDeEdicionDeFilasModeloDeVista.cs` | Hay que recargar y perder los demás cambios. |
| B-33 | Exportar: tipos de parámetros tabla en "Generar EXEC" sin esquema | `GeneradorDeScriptsSqlServer.cs` (`GenerarEjecucionDeProcedimiento`) | `DECLARE @p TipoTabla` sin `dbo.`. |
| B-34 | Restaurar con WITH MOVE renombra archivos FILESTREAM como `.ndf` | `AdministradorDeRespaldosSqlServer.cs` (`UbicarArchivos`) | Raro; revisar al probar FILESTREAM. |
| B-35 | Expandir una base sin acceso (HAS_DBACCESS = 0) da error en el árbol | `ConsultasDeCatalogo/ListarBasesDeDatos.sql` | Se muestra como en línea pero no se puede abrir. |
| B-36 | Restaurar: ventana entre SINGLE_USER y RESTORE (lotes distintos) | `AdministradorDeRespaldosSqlServer.cs` (`GenerarScriptDeRestauracion`) | Otra conexión puede tomar el único lugar. |
| B-37 | Diseño de tablas deja `SET XACT_ABORT ON` en la sesión si el script se abre en una pestaña | `GeneradorDeScriptsSqlServer.cs` (`GenerarCambiosDeTabla`) | Cambia el comportamiento de las consultas siguientes en esa pestaña. |
| B-38 | Historial guarda en texto plano scripts con contraseñas | `Aplicacion/Consultas/HistorialDeConsultas.cs` | Ej. `CREATE LOGIN ... PASSWORD`. Carpeta privada, pero conviene filtrarlo. |
| B-39 | Ejecutar con muchos mensajes (bucles con miles de INSERT) es lento | `Escritorio/ModelosDeVista/Resultados/ResultadosModeloDeVista.cs` (`Mostrar`) | Se agregan uno por uno a la lista. |
| B-54 | Exportar no crea los tipos tabla (`CREATE TYPE ... AS TABLE`) | `ConsultasDeCatalogo/ListarTiposDeUsuario.sql` excluye `is_table_type` | Los procedimientos con parámetros de tipo tabla fallan al recrearse. Tampoco se exportan tipos CLR. Los tipos de usuario fuera de `dbo` se usan sin esquema en las columnas. |

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
