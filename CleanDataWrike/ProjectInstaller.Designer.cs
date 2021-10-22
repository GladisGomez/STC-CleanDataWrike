namespace CleanDataWrike
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de componentes

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServiceCleanDataWrikeInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // ServiceCleanDataWrikeInstaller
            // 
            this.ServiceCleanDataWrikeInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ServiceCleanDataWrikeInstaller.Password = null;
            this.ServiceCleanDataWrikeInstaller.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = "Servicio encargado de limpiar las tareas en ambiente Test o Productivo.";
            this.serviceInstaller1.DisplayName = "STCCleanDataWrike";
            this.serviceInstaller1.ServiceName = "STCCleanDataWrike";
            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ServiceCleanDataWrikeInstaller,
            this.serviceInstaller1});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ServiceCleanDataWrikeInstaller;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}