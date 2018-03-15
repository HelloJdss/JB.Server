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
        private bool isBossCreated = false;

        //
        private const int jewelNum = 50;
        //key����ţ� value������
        Dictionary<int, JewelType> jewelMap = new Dictionary<int, JewelType>();

        public Fight(UInt64 fid)
        {
            fightID = fid;
            players = new List<Player>();
            for (int i = 1; i <= jewelNum; i++)
                jewelMap.Add(i, JewelType.NONE);
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
                //GameOver
                //1 ����ҹ㲥��Ϸ������Ϣ
                SimpleSync(NetCmd.S2C_GAME_OVER, new byte[0]);
                //2 ���㲢�㲥���
                jobTimer.Close();
                return;
            }

            //TODO��ͬ�������Ϣ

            if(now - lastNow >= 1000)
            {
                //1 ��ͻ���ͬ��ʣ��ʱ�� �������
                int left = (int)((startTime + gameDuration - now) / 1000);
                
                lastNow += 1000;
            }
            

            if(now - startTime >= createBossDelay && !isBossCreated)
            {
                isBossCreated = true;
                //��ͻ���ͬ�� ֪ͨ����boss
                SimpleSync(NetCmd.S2C_CREATE_BOSS, new byte[0]);
            }

            if(now >= nextTimeCreateJewel)
            {
                nextTimeCreateJewel = now + createJewelInterval;
                //1 CreateJewel
                //2 ����ҹ㲥jewel��λ�ã�����ʯ���ͬ��
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

        private void SimpleSync(NetCmd cmd, byte[] data)
        {
            foreach (Player player in players)
                player.GetSocket.Send(MessageHelper.PackData(cmd, data));
        }

        //Ϊ�˷��㣬���㷿��ֻ��һ���ˣ�Ҳ����ͬ��
        private void SyncFightingPlayerInfo()
        {
            //ͬ��λ�á�Ѫ������ǰ����,��ҵ�ǰ��������:S2C_SYNC_FIGHT_INFO
            //TODO:ͬ������
        }

        public byte[] PackFightData()
        {
            //��ս���ڵ���Ϣ��ͨ��MessageHelper����ɿ����л�������
            return null;
        }
    }
}