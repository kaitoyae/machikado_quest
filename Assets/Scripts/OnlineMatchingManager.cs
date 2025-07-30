using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class OnlineMatchingManager : MonoBehaviourPunCallbacks
{    
    bool isEnterRoom; // 部屋に入ってるかどうかのフラグ
    bool isMatching; // マッチング済みかどうかのフラグ

    void Start()
    {
        // マルチプレイモードの場合のみマッチング開始
        if (GameModeManager.Instance != null && 
            GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi)
        {
            Debug.Log("Starting online matching...");
            StartMatching();
        }
    }

    public void StartMatching()
    {
        // PhotonServerSettingsの設定内容を使ってマスターサーバーへ接続する
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnMatchingButton()
    {
        StartMatching();
    }

    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master server");
        // ランダムマッチング
        PhotonNetwork.JoinRandomRoom();
    }

    // ゲームサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room");
        isEnterRoom = true;
    }

    // 失敗した場合
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No random room found, creating new room");
        PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = 2 }, TypedLobby.Default);
    }

    // もし二人ならゲームを開始する
    private void Update()
    {
        if (isMatching) return;

        if (isEnterRoom)
        {
            if (PhotonNetwork.CurrentRoom.MaxPlayers == PhotonNetwork.CurrentRoom.PlayerCount)
            {
                isMatching = true;
                Debug.Log("マッチング成功");
                
                // GameManagerCardBattleのStartGameメソッドを呼び出し
                if (GameManagerCardBattle.instance != null)
                {
                    GameManagerCardBattle.instance.StartGame();
                }
            }
        }
    }
}