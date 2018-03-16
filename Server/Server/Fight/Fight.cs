using Common;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Server
{
    //ս��
    class Fight
    {
        public const int Capacity = 10;
        public UInt64 fightID;
        private List<Player> players;
        public List<Player> GetPlayers => players;
        private Timer jobTimer;
        private const double  gameDuration = 10 * 60 * 1000; //��Ϸʱ������ʱ 10����
        private const double createBossDelay = 2 * 60 * 1000; //����Boss��ʱ�� 2����
        private const double createJewelInterval = 30 * 1000; //���ɱ�ʯ��ʱ�� 30s
        private const double syncPlayerInfoInterval = 50; //�����Ϣͬ����ʱ�� 0.05s
        private const double baseInterval = syncPlayerInfoInterval;

        private double startTime = 0; //��Ϸ����ʱ��
        private double lastNow = 0;
        private double nextTimeCreateJewel;//�´����ɱ�ʯ��ʱ��
        private ST_BOSS_INFO boss = null;
        private bool isBossCreated = false;
        private bool isBossAlive = false;

        private const int jewelNum = 50;
        //key����ţ� value������
        Dictionary<int, JewelType> jewelMap = new Dictionary<int, JewelType>();

        public Fight(UInt64 fid)
        {
            fightID = fid;
            players = new List<Player>();
            for (int i = 1; i <= jewelNum; i++)
                jewelMap.Add(i, JewelType.NONE);

            _CreateJewelAndSync(false);
        }
        public void Start()
        {
            jobTimer = new Timer
            {
                Interval = baseInterval
            };
            jobTimer.Elapsed += (s, e) => _TimerJob();
            startTime = DateTime.Now.Millisecond; //��ʼ����ʱ��
            nextTimeCreateJewel = startTime;
            lastNow = startTime;
            jobTimer.Start();
        }

        private void _TimerJob()
        {
            double now = DateTime.Now.Millisecond;
            if (now - startTime >= gameDuration)
            {
                _FightOver();
                jobTimer.Close();
                return;
            }
            //ͬ�������Ϣ
            SyncFightInfo();

            if (now - lastNow >= 1000)
            {
                //��ͻ���ͬ��ʣ��ʱ�� �������
                int remaining = (int)((startTime + gameDuration - now) / 1000);
                ST_REMAINING_TIME rt = new ST_REMAINING_TIME(remaining);

                _SimpleSync(NetCmd.S2C_SYNC_REMAINING_TIME, MessageHelper.SerializeToBinary(rt));

                lastNow += 1000;
            }
            
            if(now - startTime >= createBossDelay && !isBossCreated)
            {
                isBossCreated = true;
                isBossAlive = true;

                boss = new ST_BOSS_INFO(1000);

                _SimpleSync(NetCmd.S2C_CREATE_BOSS, MessageHelper.SerializeToBinary(boss));
            }

            if(now >= nextTimeCreateJewel)
            {
                nextTimeCreateJewel = now + createJewelInterval;

                _CreateJewelAndSync(true);
            }
        }


        public void AddPlayer(Player player)
        {
            lock (players)
            {
                if (players.Count < Capacity)
                {
                    player.InGame = true;
                    players.Add(player);
                }
            }
        }

        public void RemovePlayer(Player player)
        {
            lock (players)
            {
                if(players.Contains(player))
                {
                    player.InGame = false;
                    players.Remove(player);
                }
            }
        }

        private void _SimpleSync(NetCmd cmd, byte[] data)
        {
            foreach (var player in players)
                player.GetSocket.Send(MessageHelper.PackData(cmd, data));
        }

        //Ϊ�˷��㣬���㷿��ֻ��һ���ˣ�Ҳ����ͬ��
        private void SyncFightInfo()
        {
            _SimpleSync(NetCmd.S2C_SYNC_FIGHT_INFO, PackFightData());
        }

        public byte[] PackFightData()
        {
            //��ս���ڵ���Ϣ��ͨ��MessageHelper����ɿ����л�������
            
            ST_FIGHT_INFO fightInfo = new ST_FIGHT_INFO(fightID, boss, _PackAllPlayerData(), _PackJewelData());

            return MessageHelper.SerializeToBinary(fightInfo);
        }
        //�����ʯ��Ϣ
        private ST_JEWEL_INFO[] _PackJewelData()
        {
            List<ST_JEWEL_INFO> jEWEL_INFOs = new List<ST_JEWEL_INFO>();

            foreach(var item in jewelMap)
            {
                if(item.Value != JewelType.NONE)
                {
                    jEWEL_INFOs.Add(new ST_JEWEL_INFO(item.Key, item.Value));
                }
            }
            if (jEWEL_INFOs.Count > 0)
                return jEWEL_INFOs.ToArray();
            return null;
        }
        //������ս����Ϣ
        private ST_PLAYER_FIGHT_INFO[] _PackAllPlayerData()
        {
            List<ST_PLAYER_FIGHT_INFO> pLAYER_INFOs = new List<ST_PLAYER_FIGHT_INFO>();

            foreach(var p in players)
            {
                pLAYER_INFOs.Add(new ST_PLAYER_FIGHT_INFO(p.Username, p.FightInfo.Position,
                    p.FightInfo.Hp, p.FightInfo.Score, p.FightInfo.AnimationType, p.FightInfo.SkillPosition));
            }

            if (pLAYER_INFOs.Count > 0)
                return pLAYER_INFOs.ToArray();
            return null;
        }

        //��Ϸ����
        private void _FightOver()
        {
            //��������
            List<ST_PLAYER_RECORD> records = new List<ST_PLAYER_RECORD>();
            foreach(var p in players)
            {
                p.InGame = false;
                records.Add(new ST_PLAYER_RECORD(p.Username, p.FightInfo.Score));
            }
            ST_FIGHT_OVER_RESULT result = new ST_FIGHT_OVER_RESULT(records.ToArray());
            //������㲥���ͻ���
            _SimpleSync(NetCmd.S2C_GAME_OVER, MessageHelper.SerializeToBinary(result));
            players.ToArray()[0].GetGameServer.GetFightList.Remove(this);
            players.Clear();
            jewelMap.Clear();
        }

        private void _CreateJewelAndSync(bool shouldSync)
        {
            List<ST_JEWEL_INFO> newJewels = new List<ST_JEWEL_INFO>();
            foreach(var j in jewelMap)
            {
                if(j.Value == JewelType.NONE)
                {
                    newJewels.Add(new ST_JEWEL_INFO(j.Key, JewelType.RED_JEWEL));
                }
            }
            if (shouldSync)
                _SimpleSync(NetCmd.S2C_CREATE_JEWEL, MessageHelper.SerializeToBinary(newJewels.ToArray()));
        }
    }
}