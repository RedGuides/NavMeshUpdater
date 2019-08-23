using System.Threading;
using System.Windows.Forms;

namespace NavMeshUpdater
{
    public partial class SplashScreen : Form
    {
        private delegate void CloseDelegate();

        //The type of form to be displayed as the splash screen.
        private static SplashScreen splashForm;

         public static void ShowSplashScreen()
        {
            // Make sure it is only launched once.

            if (splashForm != null)
                return;
            Thread thread = new Thread(new ThreadStart(SplashScreen.ShowForm));
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

         private static void ShowForm()
        {
            splashForm = new SplashScreen();
            Application.Run(splashForm);
        }

         public static void CloseForm() => splashForm.Invoke(new CloseDelegate(SplashScreen.CloseFormInternal));
        

         private static void CloseFormInternal()
        {
            splashForm.Close();
            splashForm = null;
        }

        public SplashScreen()
        {
            InitializeComponent();
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => Main.OpenURL(Main.updaterJsonURL);
        
    }
}
