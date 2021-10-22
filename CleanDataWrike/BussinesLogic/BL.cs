using CleanDataWrike.Entities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilitiesPortable.Entities;

namespace CleanDataWrike.BussinesLogic
{
    public class BL
    {
        #region Properties
        private readonly string urlDeleteTaskWrike = string.Empty;
        private readonly string urlGetTaskByFolderIdAndCreateDate = string.Empty;
        private readonly int totalRequestsToWrike = 0;
        private readonly string tokenWrike = string.Empty;
        private readonly string connDBTest = string.Empty;
        private readonly string connDBProd = string.Empty;
        private readonly string foldersToIgnore = string.Empty;
        private readonly string orderToClean = string.Empty;
        private readonly bool isTest = false;
        private List<string> dataNoDelete = new List<string>();
        private List<string> dataDelete = new List<string>();
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private int intentos = 0;
        private readonly int createdDateToBehind = 0;

        #endregion

        #region Constructor
        public BL()
        {
            urlDeleteTaskWrike = System.Configuration.ConfigurationManager.AppSettings["urlDeleteTaskWrike"].ToString();
            urlGetTaskByFolderIdAndCreateDate = System.Configuration.ConfigurationManager.AppSettings["urlGetTaskByFolderIdAndCreateDate"].ToString();
            tokenWrike = System.Configuration.ConfigurationManager.AppSettings["tokenWrike"].ToString();
            totalRequestsToWrike = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["totalRequestsToWrike"].ToString());
            connDBTest = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnTest"].ToString();
            connDBProd = System.Configuration.ConfigurationManager.ConnectionStrings["DbConnProd"].ToString();
            isTest = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["isTest"].ToString());
            foldersToIgnore = System.Configuration.ConfigurationManager.AppSettings["foldersToIgnore"].ToString();
            orderToClean = System.Configuration.ConfigurationManager.AppSettings["orderToClean"].ToString();
            createdDateToBehind = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["createdDateToBehind"].ToString());


        }
        #endregion

        #region Methods

        /// <summary>
        /// Metodo encargado de gestionar el borrado de tareas en Wrike
        /// </summary>
        /// <created>Daniel Munguia</created>
        public void deleteAllAlertsInWrike()
        {
         
            ServiceController STCService = new ServiceController("STCCleanDataWrike");

            try
            {
                if (orderToClean.Contains(";"))
                {
                    string[] order = orderToClean.Split(';');
                    foreach (var item in order)
                    {
                        if (deleteTask(item))
                        {
                            _log.Info("Limpieza de {0} completa",item);
                        }
                    }
                }
                else
                {
                    if (deleteTask(orderToClean))
                    {
                        _log.Info("Limpieza de {0} completa",orderToClean);
                    }
                }

                //Se detiene el servicio
                STCService.Stop();


            }
            catch (Exception ex)
            {
                _log.Info("Error en metodo deleteAllAlertsInWrike, mensaje: {0}", ex.Message);
                _log.Info("Intento {0} por limpiar Wrike, mensaje: {1}",intentos.ToString(), ex.Message);
                if (intentos <= 3)
                {
                    intentos++;
                    deleteAllAlertsInWrike();
                }
                else
                {
                    //Se detiene servicio
                    _log.Info("Maximo de intentos alcanzado por limpiar Wrike, se detiene servicio por error , mensaje: {1}",ex.Message);
                    STCService.Stop();
                }

                //throw ex;
            }
        }



        public bool deleteTask(string STC)
        {
            
            List<string> dataFolder = new List<string>();
            bool result = false;
            try
            {
                dataFolder = getFolders(STC);
                if (dataFolder.Count > 0)
                {
                    foreach (string item in dataFolder)
                    {
                        SearchDate dates = new SearchDate();
                        dates.start = DateTime.Now.AddYears(-1);
                        dates.end = DateTime.Now.AddDays(-createdDateToBehind);
                        List<string> data = getdataWrikes(dates,item);
                        if (data.Count > 0)
                        {
                            deleteTasks(data);

                            _log.Info("Limpieza del folder:{0} completa, total de tareas eliminadas:{1}", item,data.Count().ToString());
                        }
                        //No hay tareas para eliminar en ese folder
                        _log.Info("No hay tareas para eliminar en el id folder {0}", item);
                    }

                    // Recorre e intenta por segunda vez eliminar las tareas que no se pudieron eliminar en la primera corrida
                    if (dataNoDelete.Count > 0)
                    {
                        deleteTasks(dataNoDelete);

                        if (dataNoDelete.Count > 0)
                        {
                            _log.Info("Tareas sin eliminar: [{0}]", string.Join(", ", dataNoDelete));
                        }
                    }

                    //Detiene el servicio para indicar que termino de procesar
                    _log.Info("Se termino la limpieza de Wrike, total de tareas eliminadas:{0}", dataDelete.Count().ToString());
                    result = true;
                    // service.Stop();
                }
                else
                {
                    // No hay informacion de folders 
                    _log.Info("No existen folders activos en base de datos para realizar la limpieza, revise el catalogo AlertTypes campo Active");
                }


            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo deleteAllAlertsInWrike, mensaje: {0}", ex.Message);
                throw ex;
            }
            return result;
        }


        /// <summary>
        /// Metodo encargado de elimnar tareas
        /// </summary>
        /// <param name="data"></param>
        /// <created>Daniel Munguia</created>
        public void deleteTasks(List<string> data)
        {
            try
            {
                int contador = 1;
                foreach (string item in data)
                {
                    deleteTaskInWrike(item);
                    contador++;
                    if (contador == totalRequestsToWrike)
                    {
                        contador = 1;
                        Thread.Sleep(60000);
                    }
                }


            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo deleteTasks, mensaje: {0}", ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// Metodo encargado de eliminar una tarea en Wrike
        /// </summary>
        /// <param name="request"></param>
        /// <param name="idTaskWrike"></param>
        /// <returns></returns>
        /// <created>Daniel Munguia</created>
        public void deleteTaskInWrike(string idTaskWrike)
        {
            try
            {
                string streamResult = string.Empty;
                string urlBase = string.Format("{0}{1}", urlDeleteTaskWrike, idTaskWrike);
                HttpWebRequest servicio = (HttpWebRequest)WebRequest.Create(urlBase);
                servicio.Method = "DELETE";
                servicio.Headers.Add("Authorization", "Bearer " + tokenWrike);
                servicio.ContentType = "application/json";
                string datasend = string.Empty;
                using (var stream = new StreamWriter(servicio.GetRequestStream()))
                {
                    stream.Write(datasend);
                    stream.Flush();
                    stream.Close();
                }
                HttpWebResponse response = (HttpWebResponse)servicio.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        streamResult = reader.ReadToEnd();
                    }

                    dataDelete.Add(idTaskWrike);
                }
                else
                {
                    dataNoDelete.Add(idTaskWrike);

                }
            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo deleteTaskInWrike, mensaje: {0}", ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Metodo encargado de obtener la informacion de tareas dentro de la carpeta especifica en Wrike buscando por fecha
        /// </summary>
        /// <returns>List<string></returns>
        /// <created>Daniel Munguia</created>
        public List<string> getdataWrikes(SearchDate dateToSearch, string idFolder)
        {
            List<string> result = new List<string>();
            ResponseWrike responseWrike = new ResponseWrike();
            try
            {
                string formatToSearch = "{'start':'"+ dateToSearch.start.ToString("yyyy-MM-ddTHH:mm:ssZ") + "','end':'" + dateToSearch.end.ToString("yyyy-MM-ddTHH:mm:ssZ") + "'}";
                string streamResult = string.Empty;
                string urlBase = string.Format(urlGetTaskByFolderIdAndCreateDate, idFolder, formatToSearch);
                HttpWebRequest servicio = (HttpWebRequest)WebRequest.Create(urlBase);
                servicio.Method = "GET";
                servicio.Headers.Add("Authorization", "Bearer " + tokenWrike);
                servicio.ContentType = "application/json";

                HttpWebResponse response = (HttpWebResponse)servicio.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        streamResult = reader.ReadToEnd();
                        responseWrike = JsonConvert.DeserializeObject<ResponseWrike>(streamResult);

                        foreach (ResponseWrike.Data obj in responseWrike.data)
                        {
                            result.Add(obj.id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo getdataWrikes, mensaje: {0}", ex.Message);
                throw ex;
            }

            return result;
        }

        /// <summary>
        /// Metodo encargado de obtener todos los folders de Wrike dependiendo el ambiente configuirado
        /// </summary>
        /// <returns></returns>
        /// <created>Daniel Munguia</created>
        public List<string> getFolders(string STC)
        {
            List<string> result = new List<string>();
            string query = string.Empty;
            try
            {
                if (STC.Equals("T2"))
                {
                    query = string.Format("select distinct(IdWrikeFolder) as IdFolder from AlertTypes where ActiveForCleanData = 1 and AlertType_STC != 5");

                }
                else
                {
                    query = string.Format("select distinct(IdWrikeFolder) as IdFolder from AlertTypes where ActiveForCleanData = 1 and AlertType_STC = 5");

                }

                using (MySqlConnection conn = new MySqlConnection())
                {
                    conn.ConnectionString = isTest ? connDBTest : connDBProd;
                    using (MySqlCommand command = new MySqlCommand(query, conn))
                    {

                        conn.Open();
                        MySqlDataReader myreader = command.ExecuteReader();

                        while (myreader.Read())
                        {
                            result.Add(myreader.GetString("IdFolder"));
                        }
                        conn.Close();
                    }
                }


            }
            catch (Exception ex)
            {
                _log.Error("Error en metodo getFolders, mensaje: {0}", ex.Message);
                throw ex;
            }

            return result;
        }
        #endregion
    }
}
