# YoshiSQL - Contexto para Claude

Entorno gráfico de escritorio (alternativa ligera a SSMS) para **SQL Server en Linux y Windows**.
C# + .NET 10 + Avalonia 12 (MVVM con CommunityToolkit.Mvvm). Versión actual: **0.5.0**.

Documentos de referencia en la raíz: `README.md` (funciones y uso) y `ESTRUCTURA_YOSHISQL.txt`
(arquitectura, convenciones y estado detallado). Ojo: ese archivo menciona una carpeta `docs/`
que **no existe**; toda la documentación vive en esos dos archivos.

## Comandos

```bash
dotnet build                                   # compilar (advertencias = errores)
dotnet test                                    # 129 pruebas xUnit
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

### Otros pendientes (según README / ESTRUCTURA)
- Instalador Linux (.deb / AppImage) con ícono.
- Valores `DEFAULT` en el diseñador de tablas.
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
- ⭐🟡 Mantenimiento de índices: ver fragmentación y reconstruir/reorganizar.
- 🟢 Renombrar objeto (F2, con sp_rename). PENDIENTE.
- ✅ HECHO (sin commit) 🟢 Script como SELECT / INSERT / UPDATE / DELETE desde el menú del explorador
  (tablas: los 4; vistas: SELECT). `IGeneradorDeScripts.GenerarInstruccionDml` + `TipoDeScriptDml`,
  servicio `GenerarInstruccionDmlAsync`, acciones en `FabricaDeNodos`. EXEC de procedimientos queda PENDIENTE
  (necesita leer los parámetros del procedimiento del catálogo).
- 🟡 Ejecutar procedimiento… (diálogo que pide parámetros y genera el EXEC).
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
- ⭐🔴 **Exportar toda la base a un .sql** (estructura + datos como INSERT): botón derecho en la base →
  "Exportar todo a script". Formato abierto, versionable en git, recreable en cualquier SQL Server; no
  depende de un .bak ni de un proveedor. REAPROVECHA mucho de lo ya hecho: generación de CREATE TABLE,
  generación de SELECT, exportadores e `IDivisorDeLotes`. **Empezar por esta.**
- ⭐🔴 **Comparar dos bases (esquema + datos)**: muestra tablas/columnas nuevas, distintas o borradas y
  genera el script de sincronización. SSMS no lo trae gratis → función estrella diferenciadora.
- ⭐🟡 **Panel "salud de la base"**: una sola pantalla con tablas más pesadas, índices fragmentados,
  índices que faltan (ya se analizan en los planes) y consultas más lentas.
- 🟢 **Favoritos / fragmentos de consultas personales**: guardar consultas con nombre y ejecutarlas con un clic o un atajo en el editor.

### Imagen de la app (branding) — PENDIENTE
- 🟡 Logo propio de YoshiSQL: ícono de la ventana y de la app (ItemGroup AvaloniaResource / ApplicationIcon),
  y mostrarlo en el diálogo "Acerca de" y en la pantalla de conexión.
- 🟡 Splash screen al iniciar (ventana de bienvenida con el logo mientras carga), como en SSMS/otras apps.
  En Avalonia se hace con una ventana ligera que se muestra antes de la principal o con un SplashScreen.

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

## Notas para trabajar
- Al terminar una función, actualizar la sección "ESTADO ACTUAL" de `ESTRUCTURA_YOSHISQL.txt`, el README y este archivo.
- Agregar pruebas en la capa correspondiente y mantener `dotnet build` sin advertencias.
