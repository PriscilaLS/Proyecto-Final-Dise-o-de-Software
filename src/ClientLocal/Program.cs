using System;
using System.Windows;
using ClientLocal.Services.Session;
using ClientLocal.Views.Auth;

namespace ClientLocal
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            var sessionService = new SessionService();
            var loginView = new LoginView(sessionService);
            app.Run(loginView);
        }
    }
}