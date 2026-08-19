using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Laberinto
{
    public partial class Form1 : Form
    {
        private struct Punto : IEquatable<Punto>
        {
            public readonly int Fila;
            public readonly int Columna;

            public Punto(int fila, int columna)
            {
                Fila = fila;
                Columna = columna;
            }

            public bool Equals(Punto otro)
            {
                return Fila == otro.Fila && Columna == otro.Columna;
            }

            public override bool Equals(object obj)
            {
                return obj is Punto && Equals((Punto)obj);
            }

            public override int GetHashCode()
            {
                return Fila * 31 + Columna;
            }

            public override string ToString()
            {
                return Fila.ToString() + Columna.ToString();
            }
        }

        private struct Direccion
        {
            public readonly int DeltaFila;
            public readonly int DeltaColumna;

            public Direccion(int deltaFila, int deltaColumna)
            {
                DeltaFila = deltaFila;
                DeltaColumna = deltaColumna;
            }
        }

        private const int Tamano = 10;
        private const int TamanoCelda = 40;
        private const int MaxIntentosGeneracion = 500;

        // Arriba, Abajo, Izquierda, Derecha (el nombre coincide con el movimiento real)
        private static readonly Direccion[] Direcciones = new[]
        {
            new Direccion(-1, 0),
            new Direccion(1, 0),
            new Direccion(0, -1),
            new Direccion(0, 1),
        };

        // --- Paleta institucional (rojo predominante, estilo UAI) ---
        private static readonly Color ColorPrimario = Color.FromArgb(200, 16, 46);
        private static readonly Color ColorPrimarioOscuro = Color.FromArgb(140, 8, 30);
        private static readonly Color ColorAcentoDorado = Color.FromArgb(255, 193, 7);
        private static readonly Color ColorFondoForm = Color.FromArgb(250, 246, 244);

        // --- Paleta del tablero ---
        private static readonly Color ColorPared = Color.FromArgb(120, 12, 28);
        private static readonly Color ColorParedClara = Color.FromArgb(168, 32, 50);
        private static readonly Color ColorLibre = Color.FromArgb(255, 253, 250);
        private static readonly Color ColorCelda = Color.FromArgb(232, 214, 210);
        private static readonly Color ColorInicio = ColorPrimario;
        private static readonly Color ColorFin = ColorAcentoDorado;
        private static readonly Color ColorCola = Color.FromArgb(25, 118, 210);   // BFS: azul
        private static readonly Color ColorPila = Color.FromArgb(123, 31, 162);   // DFS: violeta

        private readonly int[,] laberinto = new int[Tamano, Tamano];
        private readonly Punto inicio = new Punto(0, 0);
        private readonly Punto fin = new Punto(Tamano - 1, Tamano - 1);
        private readonly Timer timerAnimacion;

        private List<Punto> caminoCola;
        private List<Punto> caminoPila;
        private List<Punto> ordenColaVisitados;
        private List<Punto> ordenPilaVisitados;
        private int pasoAnimacion;
        private bool animando;

        public Form1()
        {
            InitializeComponent();

            timerAnimacion = new Timer { Interval = 20 };
            timerAnimacion.Tick += TimerAnimacion_Tick;

            GenerarLaberinto();
        }

        /// <summary>
        /// Genera celdas aleatorias y reintenta hasta obtener un laberinto con
        /// camino garantizado entre inicio y fin (o hasta agotar los intentos).
        /// </summary>
        private void GenerarLaberinto()
        {
            var rand = new Random();
            int intentos = 0;

            do
            {
                for (int i = 0; i < Tamano; i++)
                {
                    for (int j = 0; j < Tamano; j++)
                    {
                        laberinto[i, j] = rand.Next(1, 11) > 3 ? 1 : 0;
                    }
                }

                laberinto[inicio.Fila, inicio.Columna] = 1;
                laberinto[fin.Fila, fin.Columna] = 1;

                intentos++;
            }
            while (!ExisteCamino() && intentos < MaxIntentosGeneracion);
        }

        /// <summary>
        /// BFS de validacion: solo comprueba conectividad inicio-fin, no guarda
        /// el camino ni se muestra en pantalla.
        /// </summary>
        private bool ExisteCamino()
        {
            var visitados = new HashSet<Punto> { inicio };
            var pendientes = new Queue<Punto>();
            pendientes.Enqueue(inicio);

            while (pendientes.Count > 0)
            {
                var actual = pendientes.Dequeue();
                if (actual.Equals(fin))
                {
                    return true;
                }

                foreach (var direccion in Direcciones)
                {
                    int fila = actual.Fila + direccion.DeltaFila;
                    int columna = actual.Columna + direccion.DeltaColumna;

                    if (fila < 0 || fila >= Tamano || columna < 0 || columna >= Tamano)
                    {
                        continue;
                    }

                    if (laberinto[fila, columna] != 1)
                    {
                        continue;
                    }

                    var vecino = new Punto(fila, columna);
                    if (visitados.Contains(vecino))
                    {
                        continue;
                    }

                    visitados.Add(vecino);
                    pendientes.Enqueue(vecino);
                }
            }

            return false;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            DibujarLaberinto(e.Graphics);
        }

        private void pnlLeyenda_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DibujarMuestraLeyenda(e.Graphics, 4, ColorCola, false, "Cola (BFS)");
            DibujarMuestraLeyenda(e.Graphics, 180, ColorPila, true, "Pila (DFS)");
        }

        /// <summary>Dibuja un tramo de linea (igual estilo que la ruta final) junto a su nombre, a modo de leyenda.</summary>
        private static void DibujarMuestraLeyenda(Graphics g, int x, Color color, bool discontinua, string texto)
        {
            using (var pluma = new Pen(color, 3))
            using (var brochaTexto = new SolidBrush(Color.FromArgb(70, 70, 70)))
            using (var fuente = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                pluma.StartCap = LineCap.Round;
                pluma.EndCap = LineCap.ArrowAnchor;
                if (discontinua)
                {
                    pluma.DashStyle = DashStyle.Dash;
                }

                g.DrawLine(pluma, x, 13, x + 34, 13);
                g.DrawString(texto, fuente, brochaTexto, x + 42, 2);
            }
        }

        private void DibujarLaberinto(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var hatchPared = new HatchBrush(HatchStyle.DiagonalBrick, ColorPared, ColorParedClara))
            using (var brochaLibre = new SolidBrush(ColorLibre))
            using (var lapizCelda = new Pen(ColorCelda))
            {
                for (int i = 0; i < Tamano; i++)
                {
                    for (int j = 0; j < Tamano; j++)
                    {
                        var rect = new Rectangle(j * TamanoCelda, i * TamanoCelda, TamanoCelda, TamanoCelda);
                        g.FillRectangle(laberinto[i, j].Equals(1) ? (Brush)brochaLibre : hatchPared, rect);
                        g.DrawRectangle(lapizCelda, rect);
                    }
                }
            }

            // Nodos explorados durante la animacion (marca de "huella" pequeña, no toda la celda)
            int visibleCola = ordenColaVisitados == null ? 0 : Math.Min(pasoAnimacion, ordenColaVisitados.Count);
            for (int k = 0; k < visibleCola; k++)
            {
                DibujarHuella(g, ordenColaVisitados[k], ColorCola, -4, -4);
            }

            int visiblePila = ordenPilaVisitados == null ? 0 : Math.Min(pasoAnimacion, ordenPilaVisitados.Count);
            for (int k = 0; k < visiblePila; k++)
            {
                DibujarHuella(g, ordenPilaVisitados[k], ColorPila, 4, 4);
            }

            if (!animando)
            {
                DibujarRuta(g, caminoCola, ColorCola, false);
                DibujarRuta(g, caminoPila, ColorPila, true);
            }

            var rectInicio = new Rectangle(inicio.Columna * TamanoCelda, inicio.Fila * TamanoCelda, TamanoCelda, TamanoCelda);
            var rectFin = new Rectangle(fin.Columna * TamanoCelda, fin.Fila * TamanoCelda, TamanoCelda, TamanoCelda);

            using (var brochaInicio = new SolidBrush(ColorInicio))
            using (var brochaFin = new SolidBrush(ColorFin))
            {
                g.FillEllipse(brochaInicio, rectInicio);
                g.FillEllipse(brochaFin, rectFin);
            }

            DibujarBirrete(g, rectInicio, Color.White);
            DibujarBandera(g, rectFin, Color.FromArgb(90, 45, 0), Color.White);
        }

        /// <summary>Icono de birrete de graduacion (simbolo de inicio, en vez del clasico cuadrado verde).</summary>
        private static void DibujarBirrete(Graphics g, Rectangle celda, Color color)
        {
            int cx = celda.X + celda.Width / 2;
            int cy = celda.Y + celda.Height / 2;
            int r = celda.Width / 2 - 4;

            using (var brocha = new SolidBrush(color))
            using (var pluma = new Pen(color, 2))
            {
                var tapa = new[]
                {
                    new Point(cx, cy - r),
                    new Point(cx + r, cy - 1),
                    new Point(cx, cy + r - 5),
                    new Point(cx - r, cy - 1),
                };
                g.FillPolygon(brocha, tapa);
                g.FillRectangle(brocha, cx - r / 2, cy - 1, r, r / 2);
                g.DrawLine(pluma, cx + r - 3, cy - 1, cx + r - 3, cy + r - 3);
                g.FillEllipse(brocha, cx + r - 5, cy + r - 5, 4, 4);
            }
        }

        /// <summary>Icono de bandera de meta (simbolo de fin, en vez del clasico cuadrado rojo).</summary>
        private static void DibujarBandera(Graphics g, Rectangle celda, Color colorAsta, Color colorTela)
        {
            int cx = celda.X + celda.Width / 2;
            int cy = celda.Y + celda.Height / 2;
            int alto = celda.Height / 2 - 3;

            using (var plumaAsta = new Pen(colorAsta, 2))
            using (var brochaAsta = new SolidBrush(colorAsta))
            using (var brochaTela = new SolidBrush(colorTela))
            {
                g.DrawLine(plumaAsta, cx - 5, cy - alto, cx - 5, cy + alto);

                var tela = new[]
                {
                    new Point(cx - 5, cy - alto),
                    new Point(cx + alto, cy - alto + 4),
                    new Point(cx - 5, cy - alto + 9),
                };
                g.FillPolygon(brochaTela, tela);
                g.FillEllipse(brochaAsta, cx - 7, cy + alto - 3, 4, 4);
            }
        }

        /// <summary>Pequeña huella circular que marca una celda explorada por el algoritmo.</summary>
        private static void DibujarHuella(Graphics g, Punto p, Color color, int dx, int dy)
        {
            int cx = p.Columna * TamanoCelda + TamanoCelda / 2 + dx;
            int cy = p.Fila * TamanoCelda + TamanoCelda / 2 + dy;
            using (var brocha = new SolidBrush(Color.FromArgb(170, color)))
            {
                g.FillEllipse(brocha, cx - 4, cy - 4, 8, 8);
            }
        }

        /// <summary>Dibuja el camino solucion como una linea conectada (con flecha) en vez de resaltar celdas.</summary>
        private static void DibujarRuta(Graphics g, List<Punto> camino, Color color, bool discontinua)
        {
            if (camino == null || camino.Count < 2)
            {
                return;
            }

            var puntos = new Point[camino.Count];
            for (int i = 0; i < camino.Count; i++)
            {
                puntos[i] = new Point(
                    camino[i].Columna * TamanoCelda + TamanoCelda / 2,
                    camino[i].Fila * TamanoCelda + TamanoCelda / 2);
            }

            using (var pluma = new Pen(color, 3))
            {
                pluma.StartCap = LineCap.Round;
                pluma.EndCap = LineCap.ArrowAnchor;
                pluma.LineJoin = LineJoin.Round;
                if (discontinua)
                {
                    pluma.DashStyle = DashStyle.Dash;
                }

                g.DrawLines(pluma, puntos);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timerAnimacion.Stop();
            animando = false;

            GenerarLaberinto();

            listBox1.Items.Clear();
            listBox2.Items.Clear();
            label2.Text = "";
            label3.Text = "";
            lblResumen.Text = "";
            caminoCola = null;
            caminoPila = null;
            ordenColaVisitados = null;
            ordenPilaVisitados = null;
            pasoAnimacion = 0;

            pictureBox1.Invalidate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timerAnimacion.Stop();

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            List<Punto> ordenCola;
            List<Punto> ordenPila;
            caminoCola = BuscarCamino(usarColaFifo: true, log: listBox1, estadoLabel: label2, ordenVisitados: out ordenCola);
            caminoPila = BuscarCamino(usarColaFifo: false, log: listBox2, estadoLabel: label3, ordenVisitados: out ordenPila);

            ordenColaVisitados = ordenCola;
            ordenPilaVisitados = ordenPila;

            lblResumen.Text = string.Format(
                "Nodos explorados -> Cola (BFS): {0}    Pila (DFS): {1}",
                ordenCola.Count, ordenPila.Count);

            pasoAnimacion = 0;
            animando = true;
            timerAnimacion.Start();
        }

        private void TimerAnimacion_Tick(object sender, EventArgs e)
        {
            pasoAnimacion++;

            int max = Math.Max(
                ordenColaVisitados != null ? ordenColaVisitados.Count : 0,
                ordenPilaVisitados != null ? ordenPilaVisitados.Count : 0);

            if (pasoAnimacion >= max)
            {
                timerAnimacion.Stop();
                animando = false;
            }

            pictureBox1.Invalidate();
        }

        /// <summary>
        /// Recorre el laberinto con la misma logica para BFS (Cola/FIFO) y DFS (Pila/LIFO);
        /// solo cambia el extremo de la frontera desde el que se extrae el siguiente nodo.
        /// Guarda padre[vecino] = actual al encolar/apilar y, al llegar al destino,
        /// reconstruye el camino real siguiendo esos padres hacia atras.
        /// </summary>
        private List<Punto> BuscarCamino(bool usarColaFifo, ListBox log, Label estadoLabel, out List<Punto> ordenVisitados)
        {
            var padre = new Dictionary<Punto, Punto>();
            var visitados = new HashSet<Punto> { inicio };
            ordenVisitados = new List<Punto> { inicio };
            var frontera = new LinkedList<Punto>();
            frontera.AddLast(inicio);

            bool encontrado = false;

            while (frontera.Count > 0 && !encontrado)
            {
                log.Items.Add(string.Join(",", frontera.Select(p => p.ToString())));

                Punto actual;
                if (usarColaFifo)
                {
                    actual = frontera.First.Value;
                    frontera.RemoveFirst();
                }
                else
                {
                    actual = frontera.Last.Value;
                    frontera.RemoveLast();
                }

                foreach (var direccion in Direcciones)
                {
                    int fila = actual.Fila + direccion.DeltaFila;
                    int columna = actual.Columna + direccion.DeltaColumna;

                    if (fila < 0 || fila >= Tamano || columna < 0 || columna >= Tamano)
                    {
                        continue;
                    }

                    if (laberinto[fila, columna] != 1)
                    {
                        continue;
                    }

                    var vecino = new Punto(fila, columna);
                    if (visitados.Contains(vecino))
                    {
                        continue;
                    }

                    visitados.Add(vecino);
                    ordenVisitados.Add(vecino);
                    padre[vecino] = actual;
                    frontera.AddLast(vecino);

                    if (vecino.Equals(fin))
                    {
                        encontrado = true;
                        break;
                    }
                }
            }

            log.Items.Add(string.Join(",", frontera.Select(p => p.ToString())));

            if (!encontrado)
            {
                estadoLabel.Text = "Solucion no encontrada";
                return null;
            }

            var camino = new List<Punto>();
            var nodo = fin;
            camino.Add(nodo);
            while (!nodo.Equals(inicio))
            {
                nodo = padre[nodo];
                camino.Add(nodo);
            }
            camino.Reverse();

            estadoLabel.Text = string.Format("Solucion encontrada - {0} pasos", camino.Count - 1);
            return camino;
        }
    }
}
