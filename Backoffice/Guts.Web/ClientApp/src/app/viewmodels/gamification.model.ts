export interface IStudentBadgeModel {
  badgeType: number;
  name: string;
  description: string;
  icon: string;
  earnedAt: string;
}

export interface IRecentXpEventModel {
  description: string;
  xpEarned: number;
  createdAt: string;
}

export interface IRecentXpGainModel {
  totalXpGained: number;
  previousVisitDate?: string | null;
  events: IRecentXpEventModel[];
}

export interface IStudentGamificationProfileModel {
  userId: number;
  firstName?: string;
  lastName?: string;
  courseId: number;
  periodId: number;
  showInLeaderBoard: boolean;
  usePseudonym: boolean;
  pseudonym: string;
  avatar: string;
  classGroupId?: number | null;
  classGroupName?: string | null;
  totalXp: number;
  level: number;
  levelTitle: string;
  currentWeeklyStreak: number;
  bestWeeklyStreak: number;
  isActiveThisWeek: boolean;
  totalGreenTests: number;
  lastLeaderboardVisitDate?: string | null;
  recentGains?: IRecentXpGainModel | null;
  earnedBadges: IStudentBadgeModel[];
}

export interface ILeaderboardEntryModel {
  rank: number;
  userId: number;
  displayName: string;
  avatar: string;
  classGroupName: string;
  score: number;
  level: number;
  weeklyStreak: number;
  greenTestsCount: number;
  isCurrentUser: boolean;
}

export interface IClassGroupLeaderboardEntryModel {
  rank: number;
  classGroupId: number;
  classGroupName: string;
  studentCount: number;
  totalGreenTests: number;
  averageXp: number;
  totalXp: number;
}

export interface ICourseGamificationConfigModel {
  courseId: number;
  isGamificationEnabled: boolean;
  isWeeklyStreakEnabled: boolean;
  isLeaderboardEnabled: boolean;
  isClassCompetitionEnabled: boolean;
  isTeamSynergyEnabled: boolean;
  isBadgesEnabled: boolean;
}

export interface ICourseClassGroupModel {
  id: number;
  name: string;
  courseId: number;
  periodId: number;
}

export interface IEnrollStudentModel {
  showInLeaderBoard: boolean;
  usePseudonym: boolean;
  pseudonym: string;
  avatar: string;
  classGroupId?: number | null;
}
