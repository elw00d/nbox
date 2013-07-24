using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using minifmod4net;

namespace NativeDll
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            MiniFmodModuleRegistry registry = new MiniFmodModuleRegistry();

            using ( Stream stream = Assembly.GetExecutingAssembly( ).GetManifestResourceStream( "NativeDll.test.xm" ) ) {
                byte[ ] bytes = new byte[ stream.Length ];
                stream.Read( bytes, 0, bytes.Length );

                Thread thread = new Thread( ( ) => {
                    using (MiniFmodModule module = registry.LoadFromMemory(1, bytes))
                    {
                        module.Play();

                        do
                        {
//                            Console.Clear();
//
//                            Console.WriteLine("Order = {0}", module.GetCurrentOrder());
//                            Console.WriteLine("Row = {0}", module.GetCurrentRow());
//                            Console.WriteLine("Time = {0}", module.GetCurrentTime());


                        } while (true);

                        //module.Stop();
                    }
                } );
                thread.IsBackground = true;
                thread.Start();
                
            }
        }
    }
}
