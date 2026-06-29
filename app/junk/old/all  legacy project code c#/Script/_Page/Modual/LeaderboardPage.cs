using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class LeaderboardPage : UIPage
{
    public static LeaderboardPage instance;
    public override void Awake()
    {
        instance = this;
    }
    public override void Open()
    {
        base.Open();
        Init();
    }
    public static void OnDirect(string campaign)
    {
        instance.directCampaign = campaign;
        Console.instance.OpenPage(UIPage.PageType.Leaderboard);
    }
    void Init()
    {
        Render();
    }







    [Header("Main")]
    public Animator animPage;
    public Button btnPlayGame;
    public UICtrPageDot ctrPage;
    public float cacheTime = 180;
    int maxPage;
    float time = 0;
    bool loadded = false;
    string directCampaign;
    private void Render()
    {
        loadded = false;
        btnPlayGame.SetActive(false);
        ctrPage.gameObject.SetActive(false);
        topSpend.root.SetActive(false);
        gameLeaderboard.root.SetActive(false);


        GetLeaderboard((data) =>
        {
            var gameKeys = new List<string>();
            if (data.games != null)
            {
                foreach (var kv in data.games)
                {
                    // filter game leaderboard by player amount > 0 only...
                    if (kv.Value != null && kv.Value.Count > 0)
                    {
                        gameKeys.Add(kv.Key);
                    }
                }
            }
            maxPage = gameKeys.Count + 1;
            ctrPage.gameObject.SetActive(maxPage > 1);
            ctrPage.Init(maxPage, (index) =>
            {

                if (loadded)
                    theme.SfxClick.Play();

                if (index == 0)
                {
                    OnTopspendingLeaderboard();
                }
                else
                {
                    OnGameLeaderboard(gameKeys[index - 1]);
                }
                loadded = true;
            });


            if (directCampaign.notnull())
            {
                var index = gameKeys.IndexOf(directCampaign);
                if (index != -1)
                {
                    ctrPage.OnDisplay(index + 1);
                }
            }
            directCampaign = null;

        });
        ctrPage.onNext = (i) => { animPage.Play("next", 0, 0); };
        ctrPage.onPrev = (i) => { animPage.Play("prev", 0, 0); };
    }
    void GetLeaderboard(System.Action<CRMData.LeaderboardData> done)
    {
        if (time != 0 && Time.time < time)
        {
            done?.Invoke(CRMData.LeaderboardData.current);
            return;
        }
        api.LoadingYield();
        api.onGetLeaderboard(data =>
        {
            time = Time.time + cacheTime;
            done?.Invoke(data);
        });
    }












    [Header("TopSpend")]
    public TopSpend topSpend;
    List<UIChild> listTopSpend = new List<UIChild>();
    [System.Serializable]
    public class TopSpend
    {
        public Transform root;
        public Transform empty;
        public ScrollRect scrollRect;
        public AutoResizeToChildren autoResize;
        public Transform rootContent;
        //public GameObject itemObj;
        public List<UIChild> topPlayers;
        public Transform footer;
    }
    void OnTopspendingLeaderboard()
    {
        OnTopspendingLeaderboard(CRMData.LeaderboardData.current.topspending);
    }
    void OnTopspendingLeaderboard(List<CRMData.TopspendingData> leaders)
    {
        btnPlayGame.SetActive(false);
        topSpend.root.SetActive(true);
        topSpend.empty.SetActive(leaders == null || leaders.Count <= topSpend.topPlayers.Count);
        gameLeaderboard.root.SetActive(false);


        int index = 0;
        listTopSpend.ForEach(x => x.Destroy());
        listTopSpend = new List<UIChild>();
        foreach (var data in leaders)
        {

            UIChild child = null;
            if (index < topSpend.topPlayers.Count)
            {
                child = topSpend.topPlayers[index];
            }
            else
            {
                child = UIChild.Pool(theme.leaderboardTopspendObj, topSpend.rootContent);
                child.transform.SetAsLastSibling();
                listTopSpend.Add(child);
            }


            child.Label[0].text = $"{data.rank}";
            child.Label[1].text = data.displayName;
            child.Label[2].text = data.totalSpending.ToString("#,##0");
            child.gameObject.SetActive(true);

            if (data.pictureUrl.notnull())
            {
                //** download profile image
                child.RawImages[0].name = data.pictureUrl;
                GameStore.WebGLService.Download.OnLoadImage(data.pictureUrl, (img) =>
                {
                    if (img != null && child.RawImages[0].name == img.name)
                        child.RawImages[0].texture = img;
                });
            }
            else
            {
                child.RawImages[0].texture = theme.defaultUserImage;
            }

            //** rank
            if (data.tier != null)
                data.tier.GetIcon(child.RawImages[1]);
            else
                child.RawImages[1].texture = CustomerTheme.instance.defaultRankImage;



            index++;

        }
        topSpend.footer.SetAsLastSibling();
        topSpend.autoResize.WaitResizeToFitChildren();

    }













    [Header("GameLeaderboard")]
    public GameLeaderboard gameLeaderboard;
    CRMData.GameCampaignData gameCampaignData;
    [System.Serializable]
    public class GameLeaderboard
    {
        public Transform root;
        public Transform headerDefault;
        public Transform headerCustome;
        public RawImage headerCustomeImage;
        public RecyclingListView theList;
        public int offsetTopEmptyAmount = 0;
        public int offsetBotEmptyAmount = 0;
        public List<Color> barColors;
        public Color getColor(int index)
        {
            return index < barColors.Count ? barColors[index] : barColors[barColors.Count - 1];
        }
    }

    public void OnGameLeaderboard(string gameId)
    {
        gameCampaignData = CRMData.GameCampaignData.Get(gameId);
        if (CRMData.LeaderboardData.current.games.ContainsKey(gameId))
            OnGameLeaderboard(CRMData.LeaderboardData.current.games[gameId]);
    }
    void OnGameLeaderboard(List<CRMData.GameLeaderboardData> leaders)
    {

        var leadersOffest = new List<CRMData.GameLeaderboardData>();
        topSpend.root.SetActive(false);
        if (leaders == null || leaders.Count == 0)
        {
            btnPlayGame.SetActive(false);
            gameLeaderboard.root.gameObject.SetActive(false);
            return;
        }
        else
        {

            gameLeaderboard.root.gameObject.SetActive(true);

            //** btn playGame
            btnPlayGame.SetActive(gameCampaignData != null);

            //** game cover image (default)
            gameLeaderboard.headerDefault.SetActive(true);
            gameLeaderboard.headerCustome.SetActive(false);
            if (gameCampaignData != null && gameCampaignData.leaderboardCoverImage.notnull())
            {
                gameLeaderboard.headerCustomeImage.DownloadTexture(gameCampaignData.leaderboardCoverImage, (img) =>
                {
                    if (img != null)
                    {
                        //** set custom cover image
                        gameLeaderboard.headerDefault.SetActive(false);
                        gameLeaderboard.headerCustome.SetActive(true);
                    }
                });
            }



            //** empty top
            gameLeaderboard.offsetTopEmptyAmount.Loop(() =>
            {
                leadersOffest.Add(null);
            });

            //** real-data
            leadersOffest.AddRange(leaders);

            //** empty bot
            gameLeaderboard.offsetBotEmptyAmount.Loop(() =>
            {
                leadersOffest.Add(null);
            });
        }

        gameLeaderboard.theList.ChildPrefab = theme.leaderboardGameObj;
        gameLeaderboard.theList.Init(leadersOffest.Count, (item, index) =>
        {

            var child = item as UIChild;
            var data = leadersOffest[index];
            if (data == null)
            {
                child.transform.localScale = Vector3.zero;
            }
            else
            {
                var normalIndex = index - gameLeaderboard.offsetTopEmptyAmount;
                child.name = $"[Lead] : {data.displayName}";
                child.Label[0].text = $"{normalIndex + 1}";
                child.Label[1].text = data.displayName;
                child.Label[2].text = data.score.ToString("#,##0");
                child.Images[0].color = gameLeaderboard.getColor(normalIndex);
                if (child.Trans.Length != 0)
                {
                    child.Trans.Close();
                    if (normalIndex < child.Trans.Length)
                        child.Trans[normalIndex].SetActive(true);
                }

                child.transform.localScale = Vector3.one;
                child.gameObject.SetActive(true);

                if (data.profile != null && data.profile.id != 0)
                {
                    child.Label[1].text = data.profile.displayName;
                    //** download profile image
                    child.RawImages[0].name = data.profile.pictureUrl;
                    GameStore.WebGLService.Download.OnLoadImage(data.profile.pictureUrl, (img) =>
                    {
                        if (img != null)
                        {
                            if (child.RawImages[0].name == img.name)
                                child.RawImages[0].texture = img;
                        }

                    });
                }
                else
                {
                    child.RawImages[0].texture = theme.defaultUserImage;
                }
            }


        });

    }
    public void OnPlayGame()
    {
        if (gameCampaignData != null)
        {
            theme.SfxClick.Play();
            GameDetailPage.Open(gameCampaignData);
        }
    }

}
