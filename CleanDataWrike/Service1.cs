using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using CleanDataWrike.BussinesLogic;
using System.Threading.Tasks;

namespace CleanDataWrike
{
    public partial class Service1 : ServiceBase
    {
        #region Global
        //Se agrega log
        private static Logger _log = LogManager.GetCurrentClassLogger();
        //private string Environment = string.Empty;
        #endregion

        public Service1()
        {
            try
            {
                InitializeComponent();
                _log.Info("##############################################");
                _log.Info("# Version servicio STCCleanDataWrike: V1.0.8 #");
                _log.Info("##############################################");
            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo: STCCleanDataWrike, mensaje de error: {0}", ex.Message);
                //throw;
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                //Proceso  de limpieza de tareas en wrike
                _log.Info("Inicia proceso de borrado de Wrikes");
                BL bl = new BL();
                //bl.deleteAllAlertsInWrike();
                var tasks = new[]
                {
                    Task.Run(() => bl.deleteAllAlertsInWrike())

                };
            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo: OnStart, mensaje de error: {0}", ex.Message);
                //throw;
            }
        }


        protected override void OnStop()
        {
            try
            {
                _log.Info("Servicio STCCleanDataWrike DETENIDO");
            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo: OnStop, mensaje de error: {0}", ex.Message);
            }
        }

        //internal void TestStartupAndStop(string[] args)
        //{
        //    this.OnStart(args);
        //    //Console.ReadLine();
        //    // this.OnStop();
        //}
    }
}
