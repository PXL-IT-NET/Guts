import { Component, Input, ChangeDetectionStrategy } from "@angular/core";
import { IStudentGamificationProfileModel } from "../../viewmodels/gamification.model";

@Component({
  selector: "app-course-gamification-badges",
  templateUrl: "./course-gamification-badges.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationBadgesComponent {
  @Input() public profile: IStudentGamificationProfileModel | null = null;

  public allBadges = [
    { type: 1, name: "First Spark", icon: "🌱", description: "Passed your first test in the course" },
    { type: 2, name: "Speed Demon", icon: "⚡", description: "Completed an assignment within 24 hours" },
    { type: 3, name: "Flawless Run", icon: "🎯", description: "Passed all tests of an assignment on first try" },
    { type: 4, name: "Clean Sweep", icon: "🧹", description: "Solved 100% of all exercises in a chapter" },
    { type: 5, name: "Night Owl", icon: "🦉", description: "Submitted a passing test between 00:00 and 05:00" },
    { type: 6, name: "Early Bird", icon: "🌅", description: "Submitted a passing test between 05:00 and 08:00" },
    { type: 7, name: "Weekend Warrior", icon: "⚔️", description: "Passed a test during the weekend" },
    { type: 8, name: "Century Club", icon: "💯", description: "Achieved 100 unique passing tests" },
    { type: 9, name: "On Fire", icon: "🔥", description: "Maintained a 4-week coding streak" },
    { type: 10, name: "Iron Will", icon: "🛡️", description: "Maintained an 8-week coding streak" },
    { type: 11, name: "Sprint Champion", icon: "👑", description: "Held #1 on the Weekly Sprint" },
    { type: 12, name: "Team Powerhouse", icon: "🤝", description: "All project team members have passing tests" },
  ];

  public isBadgeEarned(badgeType: number): boolean {
    return !!this.profile?.earnedBadges?.some((b) => b.badgeType === badgeType);
  }

  public getBadgeEarnedDate(badgeType: number): string | null {
    const found = this.profile?.earnedBadges?.find((b) => b.badgeType === badgeType);
    return found ? found.earnedAt : null;
  }
}

