/**
 * CRM Data Types and Schema Definitions
 * Mapped from C# CRMData classes for IDE Auto-complete (IntelliSense).
 */

interface InitializeData {
    defaultLanguage?: string;
    loadingBgColor?: string;
    primaryColor?: string;
    secondaryColor?: string;
    bgColor?: string;
    themeId?: string;
    skipIframeThemeColor?: boolean;
    skipInappThemeColor?: boolean;
    customAssets?: Record<string, string>;
    htmlPages?: Record<string, any>;
}

interface StoreInfo {
    storeName?: string;
    conditionURL?: string;
    logoURL?: string;
    storeCoverURL?: string;
}

interface TierData {
    id: number;
    name: string;
    level: number;
    color?: string;
    icon?: string;
    benefitCard?: string;
    description?: string;
    benefits?: string[];
    rules?: TierRuleData[];
    createdAt?: string;
    updatedAt?: string;
}

interface TierRuleData {
    ruleType: string; // "total_spending" | "total_order"
    value: string;
    includeChannels?: string;
    isUpgradeRule: number;
}

interface InfomationData {
    id: number;
    lineUserId: string;
    displayName: string;
    pictureUrl?: string;
    email?: string;
    phone?: string;
    phoneVerified: number; // 0 | 1
    points: number;
    storeName?: string;
    logoURL?: string;
    memberSince?: string;
    tier?: TierData | null;
    customFields?: Record<string, any>;
}

interface BatchData {
    points: number;
    expiresAt: string;
    daysLeft: number;
}

interface EpiringData {
    total: number;
    nextBatch?: BatchData;
    batches?: BatchData[];
}

interface PointStatistics {
    available: number;
    totalEarned: number;
    pending: number;
    redeemed: number;
    apiDeducted: number;
    expiringSoon?: EpiringData;
}

interface SpendingStatistics {
    total: number;
    currency: string;
}

interface MarketplaceStatistics {
    count: number;
    spending: number;
    points: number;
}

interface OrderStatistics {
    total: number;
    approved: number;
    pending: number;
    byMarketplace?: Record<string, MarketplaceStatistics>;
}

interface StatisticsData {
    points?: PointStatistics;
    spending?: SpendingStatistics;
    orders?: OrderStatistics;
}

interface DeductionHistoryData {
    id: number;
    transactionId: string;
    points: number;
    reason: string;
    status: string; // "completed" | "pending" | etc.
    date: string;
}

interface RedeemHistoryData {
    id: number;
    rewardId: number;
    rewardName: string;
    rewardDescription?: string;
    rewardImage?: string;
    pointsUsed: number;
    redeemedAt: string;
    bagStatus?: string; // "shipped" | "processing" | etc.
    bagQuantity: number;
}

interface UserAddressesData {
    id: number;
    contactPerson: string;
    addressLine1: string;
    addressLine2?: string;
    subdistrict?: string;
    district?: string;
    province?: string;
    postalCode?: string;
    contactPhone?: string;
    isDefault: boolean;
    createdAt?: string;
    updatedAt?: string;
}

interface CRMUserData {
    user: InfomationData;
    statistics?: StatisticsData;
    deductionHistory?: DeductionHistoryData[];
    redeemHistory?: RedeemHistoryData[];
    userAddresses?: UserAddressesData[];
}

interface GameProfile {
    lineUserId: string;
    id: number;
    displayName: string;
    pictureUrl?: string;
    email?: string;
    phone?: string;
}

interface MarketplaceData {
    name: string; // "shopee" | "lazada" | "tiktok" | etc.
    status: string; // "connected" | etc.
    connectedAt?: string;
    updatedAt?: string;
}

interface MarketReward {
    id: number;
    name: string;
    description?: string;
    termsConditions?: string;
    image?: string;
    points: number;
    quantity: number;
    rewardType: string; // "physical" | "digital" | "game"
    oneTimeRedemption: boolean;
    tierRestricted: boolean;
    tierEligible: boolean;
    requiredTiers?: TierData[];
    startDate?: string;
    endDate?: string;
    status: string; // "active" | etc.
    alreadyRedeemed: boolean;
    canRedeem: boolean;
    displayAtShop: boolean;
    gameMetadata?: {
        gameId?: string;
        itemId?: string;
        type?: string; // "item" | "currency" | "event" | "gacha"
        amount?: number;
        displayAtShop?: boolean;
    };
}

interface MarketBanner {
    id: number;
    name: string;
    description?: string;
    type: string; // "ads" | "promotion" | "banner"
    imageUrl?: string;
    bannerUrl?: string;
    displayOrder: number;
    startDate?: string;
    endDate?: string;
    createdAt?: string;
    updatedAt?: string;
    action?: Record<string, any>;
}

interface BannerData {
    marketBanners?: MarketBanner[];
    gameBanners?: any[];
}


interface LeaderboardData {
    topspending: TopspendingData[];
    games: Record<string, GameLeaderboardData[]>;
}

interface TopspendingData {
    rank: number;
    userId: number;
    totalSpending: number;
    totalOrders: number;
    totalPointsEarned: number;
    memberSince: string;
    lastOrderDate: string;
    displayName: string;
    phone: string;
    pictureUrl: string;
    tier: TierData;
}

interface GameLeaderboardData {
    index: number;
    statId: string;
    displayName: string;
    score: number;
    win: number;
    lose: number;
    lastWinTime: number;
    profile: GameProfile;
}

interface GameCampaignData {
    gameId: string;
    model?: string;
    url?: string;
    icon?: string;
    image?: string;
    leaderboardCoverImage?: string;
    assets?: Record<string, string>;
    type: string; // "game" | "gacha"
    source: string; // "bundle" | "webmodal"
    name: string;
    description?: string;
    contactCampaign?: string;
    redeemRewardId?: string;
    startAt: number; // unix timestamp
    endAt: number; // unix timestamp
    leaderboard: boolean;
    enable: boolean;
}
interface GotoGameData {
    sessionId?: string;
    gameURL?: string;
    gameId?: string;
    statId?: string;
    token?: string;
    newUser: boolean;
}

interface MarketData {
    initializeData?: InitializeData;
    storeInfo?: StoreInfo;
    setting?: any;
    rewards?: MarketReward[];
    banners?: BannerData;
    tiers?: TierData[];
    marketplaces?: MarketplaceData[];
    gameCampaigns?: GameCampaignData[];
    enableDailyCheckIn?: boolean;
    enableReferralCode?: boolean;
}



interface UserData {
    userId: string;
    type: string;
    createdAt: string;
    updatedAt: string;
    displayName: string;
    sessionToken: string;
    meta: any;
    permission: string[];
}
interface StatData {
    statId: string;
    gameConfigId: string;
    refId: string;
    createdAt: string;
    updatedAt: string;
    customProperties: any;
}

interface DailyData {
    canCheckIn: boolean;
    currentStreak: number;
    currentStamina: number;
    nextDayIndex: number;
}

interface CRMDataStore {
    user: UserData | null;
    stat: StatisticsData | null;
    crmUser: GameProfile | null;
    serverTime: number;
    publicKey: string | null;
    daily: DailyData | null;
    marketData: MarketData | null;
}


interface ResponseData {
    result: {
        code: number;
        message: string;
        data: any;
    };
}