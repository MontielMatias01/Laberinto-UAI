# Laberinto UAI

Aplicación de escritorio (WinForms, .NET Framework 4.7.2) que genera un laberinto
aleatorio de 10x10 y lo resuelve con dos algoritmos de búsqueda, mostrando el
recorrido de cada uno en tiempo real:

- **Cola (BFS)** — búsqueda en anchura, garantiza el camino más corto.
- **Pila (DFS)** — búsqueda en profundidad.

## Uso

1. Abrir `Laberinto.sln` en Visual Studio y compilar (o `MSBuild Laberinto.sln`).
2. Ejecutar `Laberinto.exe`.
3. **Generar laberinto**: crea un tablero nuevo (siempre con solución garantizada).
4. **Jugar**: resuelve el laberinto con ambos algoritmos y anima la exploración.

## Qué muestra

- El camino encontrado por cada algoritmo, dibujado como línea sobre el tablero.
- Cantidad de nodos explorados y pasos del camino, para comparar BFS vs DFS.
- Inicio y fin marcados con íconos propios (birrete / bandera).

## Cómo funciona

Ambos algoritmos parten del inicio y van explorando las celdas vecinas (arriba, abajo,
izquierda, derecha) que sean transitables y no visitadas, hasta llegar al fin. La
diferencia está en el orden en que exploran:

- **BFS (Cola)** explora primero las celdas más cercanas al inicio, expandiéndose "en
  anillos" hacia afuera. Por eso siempre encuentra el camino más corto.
- **DFS (Pila)** se mete lo más profundo posible por un camino antes de retroceder a
  probar otro. Encuentra *un* camino válido, pero no necesariamente el más corto.

El laberinto se genera al azar, pero se valida antes de mostrarlo para asegurar que
siempre exista al menos un camino entre el inicio y el fin.
