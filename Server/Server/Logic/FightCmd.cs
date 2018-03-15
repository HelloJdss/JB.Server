using Common;

namespace Server.Logic
{
    class FightCmd
    {
        public static void StartFight(Player player, byte[] data)
        {
            //��data�е�fighId��ȡ��Ӧ��fight
            ST_FIGHT_ID _FIGHT_ID = (ST_FIGHT_ID)MessageHelper.DeserializeWithBinary(data);
            Fight fight = player.GetGameServer.GetFightByFightID(_FIGHT_ID.FigthID);
            if(fight == null)
            {
                LogHelper.DEBUGLOG("No Fight: fightID [{1}]", _FIGHT_ID.FigthID);
                return;
            }
            fight.Start();
        }
    }
}