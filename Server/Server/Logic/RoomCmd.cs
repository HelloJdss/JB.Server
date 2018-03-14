using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Server.ROOM
{
    //Ŀǰ��ʱֻ������ģʽ�µķ���
    class RoomCmd
    {
        public static void CreateRoom(Player player, byte[] data)
        {
            Room room = new Room(RoomIDmaker.GetNewID(), player);

            if(player.GetGameServer.AddRoomToRelax(room))
            {
                //��������Ϣ���͵��ͻ���
                ST_PLAYER_INFO[] sT_PLAYER_INFOs = new ST_PLAYER_INFO[1];//���������������Ϣ��Ŀǰֻ�з���
                sT_PLAYER_INFOs[0] = new ST_PLAYER_INFO(player.Username, player.CoinCounts, player.DiamondCounts, player.Level,
                    player.Exp, player.ClothId);
                ST_ROOM_INFO _ROOM_INFO = new ST_ROOM_INFO(room.RoomId, room.GetCapacity, room.CurrentPlayerNum, room.HouseOwnerNumber, sT_PLAYER_INFOs);
                player.GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_CREATE_ROOM_SUCCESS, MessageHelper.SerializeToBinary(_ROOM_INFO)));
                LogHelper.DEBUGLOG("Player [{0}] create room [{1}] success!", player.Username, room.RoomId);
                return;
            }
            player.GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_CREATE_ROOM_FAILED, new byte[0]));
            LogHelper.DEBUGLOG("Player [{0}] create room failed!", player.Username);
        }

        public static void EnterRoom(Player player, byte[] data)
        {
            //��չʾ�ķ����б�ѡ��һ������:��Ҫ�ͻ��˴���roomID, ��ST_ROOM_LIST_INFO�л�ȡ
            //��Ҫ�����չʾ��ʱ������������ҵ���Ϣ
            ST_ROOM_LIST_INFO roomInfo = (ST_ROOM_LIST_INFO)MessageHelper.DeserializeWithBinary(data);
            Room room = player.GetGameServer.GetRoomByRoomID(roomInfo.roomId);
            if(room == null)
            {
                player.GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_ENTER_ROOM_FAILED, new byte[0]));
                LogHelper.DEBUGLOG("Room [{0}] not exist!", roomInfo.roomId);
                return;
            }
            lock(room)
            {
                if(room.CurrentPlayerNum == room.GetCapacity)
                {
                    player.GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_ENTER_ROOM_FAILED, new byte[0]));
                    LogHelper.DEBUGLOG("Room [{0}] is full!", roomInfo.roomId);
                    return;
                }
                else
                {
                    room.AddPlayer(player);
                    player.GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_ENTER_ROOM_SUCCESS, new byte[0]));
                    //��������Ϣͬ������ǰ�������������
                    room.SyncRoomInfoToAllPlayer();
                }
            }
        }

        public static void ExitRoom(Player player, byte[] data)
        {
            ST_ROOM_LIST_INFO roomInfo = (ST_ROOM_LIST_INFO)MessageHelper.DeserializeWithBinary(data);
            Room room = player.GetGameServer.GetRoomByRoomID(roomInfo.roomId);
            if(room != null)
            {
                lock(room)
                {
                    room.RemovePlayer(player);
                    if (room.players[room.HouseOwnerNumber] == player)
                    {
                        if(room.CurrentPlayerNum == 0)
                        {
                            room.Destory();
                            player.GetGameServer.RemoveRoomByRoomID(room.RoomId);
                            return;
                        }
                        //Ѱ����һ������
                        room.ChangeHouseOwner();
                    }
                    room.SyncRoomInfoToAllPlayer();
                }
            }
        }
    }
}
