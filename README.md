# Sistema de Reservas de Laboratorios de Informática

Trabajo Práctico Integrador Final — Programación Orientada a Objetos
Aplicación de escritorio en C# / WinForms (.NET 8), con persistencia en archivos de texto.

---

## 1. Cómo ejecutarlo en Windows

**Lo que hace falta instalar:** Visual Studio 2022 (la edición Community es gratuita).
Durante la instalación, en la pantalla de cargas de trabajo, tildar **"Desarrollo de escritorio con .NET"**.
Eso ya incluye el SDK de .NET 8 y WinForms; no hay que instalar nada más.

**Pasos:**

1. Copiar la carpeta completa `Final_POO` a la PC con Windows.
2. Abrir el archivo **`SistemaReservas.sln`** haciendo doble clic.
3. Esperar a que Visual Studio termine de cargar el proyecto (abajo a la derecha dice "Listo").
4. Presionar **F5** (o el botón verde ▶ "SistemaReservas").

La primera compilación tarda un poco más porque descarga los componentes de .NET. Las siguientes son inmediatas.

**Si algo falla:**

| Problema | Solución |
|---|---|
| "No se encuentra el SDK de .NET 8" | Instalar la carga de trabajo "Desarrollo de escritorio con .NET" desde el Visual Studio Installer. |
| El proyecto no compila apenas se abre | Menú `Compilar` → `Limpiar solución`, después `Compilar solución`. |
| Arranca pero no aparece ningún usuario | Verificar que exista la carpeta `Datos` junto al `.exe`, en `SistemaReservas\bin\Debug\net8.0-windows\Datos\`. |

**Importante sobre los datos:** los archivos `.txt` se copian automáticamente a la carpeta de compilación cada vez que se compila. La aplicación lee y escribe la copia que está en `bin\Debug\net8.0-windows\Datos\`, no los originales del proyecto. Si se quiere volver al estado inicial, alcanza con borrar la carpeta `bin` y volver a compilar.

---

## 2. Usuarios para probar

No hay contraseñas: se elige el usuario de una lista.

| Legajo | Rol | Sirve para probar |
|---|---|---|
| `AD-2024-001` | Administrador | Menú Gestión habilitado, cancelar reservas ajenas |
| `D-2024-003` | Docente | Límite de 10 reservas semanales, 60 días de anticipación |
| `A-2024-015` | Alumno | Ya tiene 3 reservas en la semana: al reservar salta el límite |

---

## 3. Qué probar en la demostración

1. **Polimorfismo visible:** entrar como Alumno y después como Administrador. El menú *Gestión* está deshabilitado para el primero y habilitado para el segundo, y el texto de permisos cambia solo.
2. **Solapamiento:** entrar como Docente, ir a *Reservas → Nueva reserva*, elegir LAB01 el 28/07/2026 de 14:00 a 16:00. Salta `ReservaSolapadaException` porque ya existe R004. Pero de 16:00 a 18:00 con otro laboratorio funciona: terminar justo cuando empieza otra no es solapamiento.
3. **Límite semanal:** entrar como `A-2024-015` (Alumno) e intentar una cuarta reserva de esa semana. Salta `LimiteReservasExcedidoException` con el límite que devuelve la propia clase `Alumno`.
4. **Permisos de cancelación:** como Alumno, *Reservas → Cancelar reserva* muestra solo las propias. Como Administrador, muestra todas.
5. **Eventos:** dejar la pantalla principal abierta, crear una reserva desde la ventana de alta y cerrarla. La grilla ya está actualizada: nadie se lo pidió, la actualizó el evento `ReservaCreada`.
6. **Archivo corrupto:** abrir `bin\Debug\net8.0-windows\Datos\reservas.txt`, romper una línea (por ejemplo, borrarle un campo) y volver a ejecutar. Aparece un aviso indicando el número de línea, y el sistema arranca igual con el resto de los datos.

---

## 4. Estructura del proyecto

```
Final_POO/
├── SistemaReservas.sln
└── SistemaReservas/
    ├── Program.cs              Punto de entrada. Crea el gestor dentro de un using.
    ├── Interfaces/
    │   ├── IEntidad.cs         Contrato de las entidades persistibles
    │   ├── IValidable.cs       Contrato de auto-validación
    │   └── IRepositorio.cs     Contrato de acceso a datos (genérico)
    ├── Modelos/
    │   ├── Usuario.cs          Clase base ABSTRACTA
    │   ├── Administrador.cs    ─┐
    │   ├── Docente.cs           ├─ Clases derivadas
    │   ├── Alumno.cs           ─┘
    │   ├── Laboratorio.cs
    │   ├── Reserva.cs
    │   └── EstadoReserva.cs    Enum
    ├── Excepciones/            8 excepciones propias, todas bajo SistemaReservasException
    ├── Repositorios/
    │   ├── RepositorioArchivo.cs   Repositorio GENÉRICO único + IDisposable
    │   └── RutasDatos.cs           Ubicación de los archivos
    ├── Servicios/
    │   ├── GestorReservas.cs   Reglas de negocio + eventos
    │   ├── ServicioReportes.cs Consultas LINQ
    │   ├── ReservaEventArgs.cs Datos que viajan con los eventos
    │   └── ReportesDto.cs      Filas para las grillas
    ├── Formularios/            7 pantallas
    └── Datos/                  laboratorios.txt · usuarios.txt · reservas.txt
```

**Nota sobre los formularios:** están escritos en un único archivo `.cs` cada uno, sin archivo `.Designer.cs` separado. Los controles se crean por código en el método `InicializarComponentes()`. Funciona igual y es más fácil de leer; la contra es que el diseñador visual de Visual Studio no muestra la pantalla. Para mover un control, se cambian sus coordenadas (`Location`) en ese método.

---

## 5. Dónde está aplicado cada tema de la rúbrica

| Tema | Puntos | Dónde verlo |
|---|---|---|
| Clases, atributos y propiedades | 10 | `Modelos/Reserva.cs` — campos privados, validación cruzada de horas en los setters, propiedades calculadas `Duracion`, `EstaVencida`, `RangoHorario` |
| Constructores y sobrecarga | 8 | `Modelos/Reserva.cs` — tres constructores encadenados con `: this(...)`; las derivadas de `Usuario` encadenan con `: base(...)` |
| Herencia y polimorfismo | 12 | `Modelos/Usuario.cs` (abstracta) + `Administrador`/`Docente`/`Alumno`. Se consume en `GestorReservas.CrearReserva()` y `CancelarReserva()` |
| Destructores / IDisposable | 8 | `Repositorios/RepositorioArchivo.cs` — `Dispose()`, `Dispose(bool)`, `~RepositorioArchivo()`. Se consume con `using` en `Program.cs` |
| Excepciones personalizadas | 10 | Carpeta `Excepciones/` — jerarquía con base común y 4 constructores cada una; `try/catch/finally` en formularios y repositorio |
| Interfaces | 10 | `IRepositorio<T>`, `IEntidad`, `IValidable`, más `IDisposable` de .NET |
| Delegados, eventos y lambda | 10 | `GestorReservas` dispara `ReservaCreada` / `ReservaCancelada`; `FormPrincipal` se suscribe. Delegado `Func<string,T>` como fábrica en el repositorio |
| Genéricos | 12 | `RepositorioArchivo<T>` — una sola clase para las tres entidades, con restricción `where T : class, IEntidad` |
| LINQ | 12 | `Servicios/ServicioReportes.cs` — `GroupBy` en `UsoPorLaboratorio()`, `join` en `RankingUsuarios()`, y las dos sintaxis (consulta y método) |
| Persistencia e integración | 8 | `RepositorioArchivo` con `StreamReader`/`StreamWriter` en bloques `using`, archivo inexistente creado solo, líneas corruptas registradas y salteadas, respaldo `.bak` |

---

## 6. Tres preguntas probables en el coloquio

**"¿Por qué `Usuario` es abstracta?"**
Porque un usuario a secas no existe en el sistema: siempre es un Administrador, un Docente o un Alumno. Al marcarla `abstract`, el compilador impide `new Usuario()` y obliga a que cada rol implemente sus propias reglas.

**"Dijiste que no usás `if` según el tipo de usuario, pero en `Usuario.CrearSegunRol()` hay un `switch`."**
Es el único lugar donde existe, y es inevitable: alguien tiene que traducir el texto `"Alumno"` que viene del archivo a un objeto de esa clase. Está aislado en un solo método (el patrón fábrica) y se le pasa al repositorio como delegado. De ahí en adelante todo el sistema trabaja con la clase base y deja que el polimorfismo resuelva.

**"¿Cómo hace un repositorio genérico para guardar una clase abstracta como `Usuario`?"**
No puede hacer `new T()` porque `Usuario` es abstracta. Por eso el repositorio recibe una fábrica en forma de delegado `Func<string, T>`. Para `Laboratorio` y `Reserva` se usa el constructor sin parámetros; para `Usuario` se le pasa `Usuario.CrearSegunRol`, que lee el campo rol de la línea y devuelve la clase concreta correcta.
