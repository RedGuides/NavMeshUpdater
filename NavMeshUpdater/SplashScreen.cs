using System.Threading;
using System.Windows.Forms;

namespace NavMeshUpdater
{
    public partial class SplashScreen : Form
    {
        private delegate void CloseDelegate();

        //The type of form to be displayed as the splash screen.
        private static SplashScreen splashForm;

        static public void ShowSplashScreen()
        {
            // Make sure it is only launched once.

            if (splashForm != null)
                return;
            Thread thread = new Thread(new ThreadStart(SplashScreen.ShowForm));
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        static private void ShowForm()
        {
            splashForm = new SplashScreen();
            Application.Run(splashForm);
        }

        static public void CloseForm() => splashForm.Invoke(new CloseDelegate(SplashScreen.CloseFormInternal));
        

        static private void CloseFormInternal()
        {
            splashForm.Close();
            splashForm = null;
        }

        public SplashScreen()
        {
            InitializeComponent();
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => Utility.OpenURL(Main.updaterJsonURL);
        
    }
}
