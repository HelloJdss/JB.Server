using Common;
using Server.ROOM;
using System.Collections.Generic;

namespace Server.Logic
{
    //ƥ������
    class MatchCmd
    {
        //ƥ�䷿��
        public static void Match(Player player, byte[] data)
        {
            //���ս����ʽ��3*Exp + 3*Level + 4*FightCount
            //��ȡ���Exp��Level��FightCount ����ս�� FP1
            //�������������з�����Exp��Level��FightCount 
            //
        }

        /**
         * ��������ģʽ�£�ֱ��ƥ��ս��,�ҵõ�δ���ļ��ɽ���ս������������ս��
         */
        public static void QuickMatch(Player player, byte[] data)
        {
            List<Fight> fights = player.GetGameServer.GetFightList;
            Fight fight = null;
            bool startNew = true;
            lock(fights)
            {
                foreach (Fight f in fights)
                {
                    if (f.GetPlayers.Count < Fight.Capacity)
                    {
                        fight = f;
                        startNew = false;
                        break;
                    }
                }
                if (fight == null)
                    fight = new Fight(IDmaker.GetNewID());
                fight.AddPlayer(player);
                
                if (startNew)
                {
                    player.GetGameServer.GetFightList.Add(fight);
                    //֪ͨ�ͻ��˿�����ս����ֻ����fightID
                    //�ͻ����յ�֪ͨ����֪ͨ����������ս������FightCmd��
                    player.GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_START_NEW_FIGHT,
                        MessageHelper.SerializeToBinary(new ST_FIGHT_ID(fight.fightID))));
                    LogHelper.DEBUGLOG("Player [{0}] start new fight [{1}].", player.Username, fight.fightID);
                }
                else
                {
                    //֪ͨ�ͻ��˽���ս��������fight��������Ϣ���ͻ��˴������
                    byte[] fightData = fight.PackFightData();
                    player.GetSocket.Send(MessageHelper.PackData(NetCmd.S2C_ENTER_FIGHT, fightData));
                    LogHelper.DEBUGLOG("Player [{0}] enter fight [{1}].", player.Username, fight.fightID);
                }
                //����û��ͬ����������Ϊÿ0.05s
                //ͬ��һ�Σ���������ҽ���ս����
                //ͬʱ����ͬ������
            }
        }

        private Room _GetSuitableRoom(Player player)
        {
            List<Room> rooms = player.GetGameServer.GetRoomList;
            if(rooms.Count == 0 || _NoSuitableRoom(rooms))
            {
                Room room = new Room(IDmaker.GetNewID(), player);
            }
            return null;
        }

        private bool _NoSuitableRoom(List<Room> rooms)
        {
            foreach(Room room in rooms)
            {
                if (room.CurrentPlayerNum < room.GetCapacity)
                    return false;
            }
            return true;
        }
    }
}