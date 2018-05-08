using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace singalR3
{
    public class PedidosHub : Hub
    {
        /*public void Send(string name, string message)
        {
            //Clients.Caller.echo(message);
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(name, message);           
        }*/
        private static ConcurrentDictionary<string, EstadoPedido> conexiones;

        public PedidosHub()
        {
            conexiones = new ConcurrentDictionary<string, EstadoPedido>();
        }
        public void IniciarRecorrido(string pedidoid)
        {
            //buscar el pedido y si es valido, voy a conectar a este cliente
            //este es llamado por el repartidor
            var estado = conexiones.GetOrAdd(pedidoid, new EstadoPedido());
            estado.RepartidorConnectionId = Context.ConnectionId;
            estado.Latitud = 0;
            estado.Longitud = 0;

            if (!string.IsNullOrWhiteSpace(estado.ClienteConnectionId))
            {
                Clients.Client(estado.ClienteConnectionId).pedidoIniciado();
            }
        }

        public void EsperarPedido(string pedidoid)
        {
            //Cliente llama a esta funcion y sera para notificar al sistema de que espera la info
            var estado = conexiones.GetOrAdd(pedidoid, new EstadoPedido());
            estado.ClienteConnectionId = Context.ConnectionId;

            if (!string.IsNullOrWhiteSpace(estado.RepartidorConnectionId))
            {
                Clients.Client(estado.RepartidorConnectionId).estoyEsperando();
            }
        }

        public void NotificarUbicacion(string pedidoID, long lat, long lon)
        {
            //repartidor notificar donde esta el pedido
            EstadoPedido edo;
            if(conexiones.TryGetValue(pedidoID, out edo))
            {
                edo.Latitud = lat;
                edo.Longitud = lon;
                if (!string.IsNullOrWhiteSpace(edo.ClienteConnectionId))
                {
                    Clients.Client(edo.RepartidorConnectionId).ubicacionCambio(edo.Latitud,edo.Longitud);
                }
            }
        }

        public void DondeEstaMiComida(string pedidoID)
        {
            //Este lo llama el cliente para pedir la ubicacion de su comida.
            EstadoPedido edo;
            if (conexiones.TryGetValue(pedidoID, out edo))
            {
                
                if (!string.IsNullOrWhiteSpace(edo.RepartidorConnectionId))
                {
                    Clients.Client(edo.RepartidorConnectionId).apurate();
                    Clients.Caller.ubicacionCambio(edo.Latitud, edo.Longitud);
                }
            }
        }
    }

    public class EstadoPedido
    {
        public string RepartidorConnectionId { get; set; }
        public string ClienteConnectionId { get; set; }
        public long Latitud { get; set; }
        public long Longitud { get; set; }

    }
}