using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService
{
    public class AzServiceBus : IAzServiceBus
    {
        private readonly ServiceBusClient _serviceBusClient;
        public AzServiceBus(ServiceBusClient service)
        {
            _serviceBusClient = service;
        }

        public async Task GetQueues(CancellationToken stoppingToken)
        {
            int bandera = 0;
            ServiceBusReceiver receptor = _serviceBusClient.CreateReceiver("primerazure");
            do
            {
                ServiceBusReceivedMessage receptorMensaje = await receptor.ReceiveMessageAsync(TimeSpan.FromMilliseconds(1000), stoppingToken);

                if (receptorMensaje != null)
                {
                    var jsonString = receptorMensaje.Body.ToString();

                    if (jsonString != null)
                    {
                        dynamic json = JsonConvert.DeserializeObject(jsonString);
                        if (json.Nombre != null && json.Apellido != null && json.NroDocumento != null)
                        {
                            await receptor.CompleteMessageAsync(receptorMensaje);

                            string cadena = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultDbConnection"].ConnectionString;
                            SqlConnection con = new SqlConnection(cadena);
                            con.Open();
                            DateTime fechaActual = DateTime.Now;
                            string fechaFormateada = fechaActual.ToString("yyy-MM-dd HH:mm:ss");
                            string querySql = "Insert into ClienteListo(Nombre, Apellido, NroDocumento, Date) Values(@Nombre, @Apellido, @NroDocumento, @Date)";
                            using (SqlCommand command = new SqlCommand(querySql, con))
                            {
                                command.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = json.Nombre;
                                command.Parameters.Add("@Apellido", SqlDbType.VarChar).Value = json.Apellido;
                                command.Parameters.Add("@NroDocumento", SqlDbType.VarChar).Value = json.NroDocumento;
                                command.Parameters.Add("@Date", SqlDbType.VarChar).Value = fechaFormateada;
                                command.ExecuteNonQuery();
                            }
                        }
                        else bandera = 1;
                    }
                    else bandera = 1;
                }
                else bandera = 1;
            } while (bandera == 0);
        }

        public async Task ProcessMessages()
        {
            string queueName = "myqueue";

            var processor = _serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions());

            processor.ProcessMessageAsync += MessageHandler;

            processor.ProcessErrorAsync += ErrorHandler;

            await processor.StartProcessingAsync();

            await Task.Delay(TimeSpan.FromSeconds(5));

            Console.WriteLine("Stopping the receiver...");
            await processor.StopProcessingAsync();
            Console.WriteLine("Stopped receiving messages");
        }

        public Task ErrorHandler(ProcessErrorEventArgs arg)
        {
            return Task.CompletedTask;
        }

        public async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                BinaryData content = args.Message.Body;
                string contentStr = content.ToString();

                if(contentStr != null)
                {
                    dynamic json = JsonConvert.DeserializeObject(contentStr);

                    if(json.Nombre != null && json.Apellido != null && json.NroDocumento != null)
                    {
                        string cadena = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultDbConnection"].ConnectionString;

                        SqlConnection con = new SqlConnection(cadena);
                        con.Open();
                        DateTime fechaActual = DateTime.Now;
                        string fechaFormateada = fechaActual.ToString("yyyy-MM-dd HH:mm:ss");
                        string querySql = "Insert into ClienteListo(Nombre, Apellido, NroDocumento, Date) Values(@Nombre, @Apellido, @NroDocumento, @Date)";
                        using(SqlCommand command = new SqlCommand(querySql, con))
                        {
                            command.Parameters.Add("@Nombre", SqlDbType.VarChar).Value = json.Nombre;
                            command.Parameters.Add("@Apellido", SqlDbType.VarChar).Value = json.Apellido;
                            command.Parameters.Add("@NroDocumento", SqlDbType.VarChar).Value = json.NroDocumento;
                            command.Parameters.Add("@Date", SqlDbType.VarChar).Value = fechaFormateada;

                            command.ExecuteNonQuery();
                        }
                    }
                }
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception)
            {
                await args.AbandonMessageAsync(args.Message);
            }
        }

    }
}
