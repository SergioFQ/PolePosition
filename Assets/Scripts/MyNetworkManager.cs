using Mirror;
using UnityEngine;


    [AddComponentMenu("")]
    public class MyNetworkManager : NetworkManager
    {
        public string PlayerName { get; set; }
    

        public class CreatePlayerMessage : MessageBase
        {
            public string name;
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            // tell the server to create a player with this name
            conn.Send(new CreatePlayerMessage { name = PlayerName });
        }
        
    }

