using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Prism.Ioc;
using Microsoft.Extensions.DependencyInjection;
using AOIClient.ViewModel;
using AOIClient.Interfaces;

namespace AOIClient
{        
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Properties&Attributes
        private readonly IHost _host;
        #endregion Properties&Attributes        


        #region Lifetime
        public App()
        {
            _host = Host.CreateDefaultBuilder()                
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IAOIChatViewModel, AOIChatViewModel>();

                    services.AddSingleton(s => new MainWindow()
                    {
                        DataContext = s.GetRequiredService< IAOIChatViewModel> ()
                    });
                })
                .Build();
        }
        #endregion Lifetime

        #region operations
        protected override void OnStartup(StartupEventArgs e)
        {
            _host.Start();
            
            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.Dispose();

            base.OnExit(e);
        }
        #endregion operations
    }
}
