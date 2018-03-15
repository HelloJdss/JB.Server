using Common;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Server.ROOM
{
    class Room
    {
        private const int capacity = 10;//��������
        public int GetCapacity => capacity;
        private UInt64 roomId;//����ID
        public UInt64 RoomId { get; set; }
        private Player houseOwner;//����
        public Player HouseOwner { get; set; }
        public int HouseOwnerNumber = 0;//������ţ���ʼΪ0
        public Player[] players;
        public int CurrentPlayerNum = 0;//��ǰ��������
        public bool isFighting = false; //��ǰ�����Ƿ���ս����,false��ʾ׼���У�trueΪս����

        

        public Room(UInt64 rid, Player houseowner)
        {
            players = new Player[capacity];
            for (int i = 0; i < capacity; i++)
                players[i] = null;

            RoomId = rid;
            HouseOwner = houseowner;
            HouseOwnerNumber = 0;
            CurrentPlayerNum = 1;
        }

        public void RemovePlayer(Player player)
        {
            for (int i = 0; i < capacity; i++)
            {
                if (players[i] == player)
                {
                    players[i] = null;
                    player.InRoom = false;
                    CurrentPlayerNum--;
                    break;
                }
            }
        }

        public void AddPlayer(Player player)
        {
            for (int i = 0; i < capacity; i++)
            {
                if (players[i] == null)
                {
                    players[i] = player;//����ǰ��Ҽ��뷿��
                    player.InRoom = true;
                    player.roomId = RoomId;
                    CurrentPlayerNum++;
                    break;
                }
            }
        }

        public void SyncRoomInfoToAllPlayer()
        {
            ST_PLAYER_INFO[] sT_PLAYER_INFOs = new ST_PLAYER_INFO[capacity];
            for (int i = 0; i < capacity; i++)
            {
                if (players[i] == null)
                {
                    sT_PLAYER_INFOs[i] = null;
                    continue;
                }
                sT_PLAYER_INFOs[i] = new ST_PLAYER_INFO(players[i].Username, players[i].CoinCounts, players[i].DiamondCounts, players[i].Level,
                    players[i].Exp, players[i].ClothId);
            }
            for (int i = 0; i < capacity; i++)
            {
                if (players[i] != null)
                    players[i].GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_SYNC_ROOM_PLAYER_INFO, MessageHelper.SerializeToBinary(sT_PLAYER_INFOs)));
            }
        }

        public void Destory()
        {
            for (int i = 0; i < capacity; i++)
            {
                if (players[i] != null)
                {
                    players[i].InRoom = false;
                    players[i] = null;
                }
            }
            players = null;
        }

        public void ChangeHouseOwner()
        {
            for (int i = 0; i < capacity; i++)
            {
                if (players[i] != null)
                {
                    HouseOwnerNumber = i;
                    break;
                }
            }
        }
    }
}