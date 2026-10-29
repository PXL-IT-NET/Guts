export interface IStudentBadgeModel {
  badgeType: number;
  name: string;
  description: string;
  icon: string;
  earnedAt: string;
}

export interface IBadgeMetadataModel {
  badgeType: number;
  type?: number;
  name: string;
  description: string;
  icon: string;
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
  tier?: number;
  tierName?: string;
  tierIcon?: string;
}

export interface IHallOfFameTierModel {
  tier: number;
  tierId?: number;
  name: string;
  icon: string;
  minRank: number;
  maxRank?: number | null;
  isCurrentTier: boolean;
  studentCount: number;
}

export interface IHallOfFameTargetStudentModel {
  userId: number;
  displayName: string;
  avatar: string;
  rank: number;
  score: number;
  xpNeededToOvertake: number;
  tierName: string;
  tierIcon: string;
}

export interface IHallOfFameStudentOptionModel {
  userId: number;
  displayName: string;
  rank: number;
  score: number;
  tierName: string;
  tierIcon: string;
}

export interface IHallOfFameLeaderboardModel {
  currentTier?: IHallOfFameTierModel | null;
  visibleTiers: IHallOfFameTierModel[];
  allTiers: IHallOfFameTierModel[];
  nextTargetStudent?: IHallOfFameTargetStudentModel | null;
  currentUserRank?: number | null;
  entries: ILeaderboardEntryModel[];
  studentOptions: IHallOfFameStudentOptionModel[];
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

export interface IAvatarModel {
  id: string;
  label: string;
  icon: string;
}

export const AVAILABLE_AVATARS: IAvatarModel[] = [
  { id: "CodeCat", label: "Code Cat", icon: "🐱" },
  { id: "DebugOwl", label: "Debug Owl", icon: "🦉" },
  { id: "BitBot", label: "Bit Bot", icon: "🤖" },
  { id: "SyntaxFox", label: "Syntax Fox", icon: "🦊" },
  { id: "LaunchRocket", label: "Launch Rocket", icon: "🚀" },
  { id: "Wizard", label: "Code Wizard", icon: "🧙" },
  { id: "JavaBeast", label: "Coffee Beast", icon: "☕" },
  { id: "SharpCoder", label: "Sharp Coder", icon: "⚡" },
  { id: "CyberNinja", label: "Cyber Ninja", icon: "🥷" },
  { id: "BugHunter", label: "Bug Hunter", icon: "🐞" },
  { id: "DevDragon", label: "Dev Dragon", icon: "🐉" },
  { id: "PixelPanda", label: "Pixel Panda", icon: "🐼" },
  { id: "CodeMonkey", label: "Code Monkey", icon: "🐵" },
  { id: "TerminalAlien", label: "Terminal Alien", icon: "👽" },
  { id: "StackUnicorn", label: "Stack Unicorn", icon: "🦄" },
  { id: "BinaryGhost", label: "Binary Ghost", icon: "👻" },
  { id: "GitOctopus", label: "Git Octopus", icon: "🐙" },
  { id: "TurboCheetah", label: "Turbo Cheetah", icon: "🐆" },
  { id: "LogicLion", label: "Logic Lion", icon: "🦁" },
  { id: "KeyboardKoala", label: "Keyboard Koala", icon: "🐨" },
];
