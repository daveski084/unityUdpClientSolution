﻿/***********************************************************************;
* Project           : udpclient-server Assignment 1
*
* Program name      : "NetworkMan.cs"
*
* Author            : Iansmathew
* 
* Student Number    : 101187910
*
* Date created      : 20/10/03
*
* Description       : Connect with EC2 server and relay player position data.
*
* Last modified     : 20/10/07
*
* Revision History  :
*
*Date        Author Ref    Revision (Date in YYYYMMDD format) 
*20/10/07    David Gasinec        Edited Script 
*
|**********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;


public class NetworkMan : MonoBehaviour
{
    public UdpClient udp; // an instance of the UDP client
    public GameObject playerGO; // our player object

    public string myAddress; // my address = (IP, PORT)
    public Dictionary<string,GameObject> currentPlayers; // A list of currently connected players
    public List<string> newPlayers, droppedPlayers; // a list of new players, and a list of dropped players
    public GameState lastestGameState; // the last game state received from server
    public ListOfPlayers initialSetofPlayers; // initial set of players to spawn
    
    public MessageType latestMessage; // the last message received from the server


    // Start is called before the first frame update
    void Start()
    {
        // Initialize variables
        newPlayers = new List<string>();
        droppedPlayers = new List<string>();
        currentPlayers = new Dictionary<string, GameObject>();
        initialSetofPlayers = new ListOfPlayers();
        // Connect to the client.
        // All this is explained in Week 1-4 slides
        udp = new UdpClient();
        Debug.Log("Connecting...");
        udp.Connect("18.191.183.229", 12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);
        InvokeRepeating("HeartBeat", 1, 1);
        InvokeRepeating("PlayerPositionUpdate", 1, 0.1f);
    }

    void OnDestroy(){
        udp.Dispose();
    }

    /// <summary>
    /// A structure that replicates our server color dictionary
    /// // updated to use XYZ.
    /// </summary>
    [Serializable]
        public struct Position
        {
            public float X;
            public float Y;
            public float Z;
        }

    /// <summary>
    /// A structure that replicates our player dictionary on server
    /// </summary>
    [Serializable]
    public class Player
    {
        public string id;
        public Position position;        
    }


    [Serializable]
    public class ListOfPlayers{
        public Player[] players;

        public ListOfPlayers(){
            players = new Player[0];
        }
    }

    [Serializable]
    public class ListOfDroppedPlayers
    {
        public string[] droppedPlayers;
    }

    /// <summary>
    /// A structure that replicates our game state dictionary on server
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int pktID;
        public Player[] players;
    }

    [Serializable]
    public class PositionUpdate
    {
        public commands cmd;
        public Position position;

    }



    /// <summary>
    /// A structure that replicates the mesage dictionary on our server
    /// </summary>
    [Serializable]
    public class MessageType{
        public commands cmd;
    }

    /// <summary>
    /// Ordererd enums for our cmd values
    /// </summary>
    public enum commands{
        PLAYER_CONNECTED,       //0
        GAME_UPDATE,            // 1
        PLAYER_DISCONNECTED,    // 2
        CONNECTION_APPROVED,    // 3
        LIST_OF_PLAYERS,        // 4
        POSITION_UPDATE
    };
    
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        // Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<MessageType>(returnData);
        
        Debug.Log(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.PLAYER_CONNECTED:
                    ListOfPlayers latestPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in latestPlayer.players){
                        newPlayers.Add(player.id);
                    }
                    break;
                case commands.GAME_UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.PLAYER_DISCONNECTED:
                    ListOfDroppedPlayers latestDroppedPlayer = JsonUtility.FromJson<ListOfDroppedPlayers>(returnData);
                    foreach (string player in latestDroppedPlayer.droppedPlayers){
                        droppedPlayers.Add(player);
                    }
                    break;
                case commands.CONNECTION_APPROVED:
                    ListOfPlayers myPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in myPlayer.players){
                        newPlayers.Add(player.id);
                        myAddress = player.id;
                    }
                    break;
                case commands.LIST_OF_PLAYERS:
                    initialSetofPlayers = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    break; 
                default:
                    Debug.Log("Error: " + returnData);
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(){
        if (newPlayers.Count > 0)
        {
            foreach (string playerID in newPlayers)
            {
                currentPlayers.Add(playerID,Instantiate(playerGO, new Vector3(0,0,0),Quaternion.identity));
                currentPlayers[playerID].name = playerID;
            }
            newPlayers.Clear();
        }
        if (initialSetofPlayers.players.Length > 0)
        {
            Debug.Log(initialSetofPlayers);
            foreach (Player player in initialSetofPlayers.players){
                if (player.id == myAddress)
                    continue;
                currentPlayers.Add(player.id, Instantiate(playerGO, new Vector3(0,0,0), Quaternion.identity));
                currentPlayers[player.id].transform.position = new Vector3(player.position.X, player.position.Y, player.position.Z);
                currentPlayers[player.id].name = player.id;
            }
            initialSetofPlayers.players = new Player[0];
        }
    }

    void UpdatePlayers(){
        if (lastestGameState.players.Length >0)
        {
            foreach (NetworkMan.Player player in lastestGameState.players)
            {
                string playerID = player.id;
                currentPlayers[player.id].transform.position = new Vector3(player.position.X, player.position.Y, player.position.Z);
            }
            lastestGameState.players = new Player[0];
        }
    }

    void DestroyPlayers(){
        if (droppedPlayers.Count > 0){
            foreach (string playerID in droppedPlayers){
                Debug.Log(playerID);
                Debug.Log(currentPlayers[playerID]);
                Destroy(currentPlayers[playerID].gameObject);
                currentPlayers.Remove(playerID);
            }
            droppedPlayers.Clear();
        }
    }
    
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void PlayerPositionUpdate()
    {
        PositionUpdate playerPosition = new PositionUpdate();
        playerPosition.cmd = commands.POSITION_UPDATE;
        playerPosition.position.X = currentPlayers[myAddress].transform.position.x;
        playerPosition.position.Y = currentPlayers[myAddress].transform.position.y;
        playerPosition.position.Z = currentPlayers[myAddress].transform.position.z;
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(playerPosition));
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}