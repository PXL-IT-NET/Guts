import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  signal,
} from "@angular/core";
import { GamificationService } from "../../services/gamification.service";
import { ToastrService } from "ngx-toastr";
import {
  ICourseGamificationConfigModel,
  ICourseClassGroupModel,
} from "../../viewmodels/gamification.model";

@Component({
  selector: "app-course-gamification-admin",
  templateUrl: "./course-gamification-admin.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationAdminComponent implements OnInit, OnChanges {
  @Input() public courseId: number = 0;
  @Input() public periodId?: number;

  @Output() public classesChanged = new EventEmitter<void>();
  @Output() public configChanged = new EventEmitter<ICourseGamificationConfigModel>();

  private readonly _config = signal<ICourseGamificationConfigModel | null>(null);
  public get config(): ICourseGamificationConfigModel | null {
    return this._config();
  }
  public set config(v: ICourseGamificationConfigModel | null) {
    this._config.set(v);
  }

  private readonly _classGroups = signal<ICourseClassGroupModel[]>([]);
  public get classGroups(): ICourseClassGroupModel[] {
    return this._classGroups();
  }
  public set classGroups(v: ICourseClassGroupModel[]) {
    this._classGroups.set(v);
  }

  private readonly _newClassName = signal<string>("");
  public get newClassName(): string {
    return this._newClassName();
  }
  public set newClassName(v: string) {
    this._newClassName.set(v);
  }

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  constructor(
    private readonly gamificationService: GamificationService,
    private readonly toastr: ToastrService,
  ) {}

  public ngOnInit(): void {
    this.loadAdminData();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if ((changes["courseId"] && !changes["courseId"].firstChange) ||
        (changes["periodId"] && !changes["periodId"].firstChange)) {
      this.loadAdminData();
    }
  }

  public loadAdminData(): void {
    if (this.courseId <= 0) return;
    this.loading = true;

    this.gamificationService.getConfig(this.courseId).subscribe((res) => {
      if (res.success && res.value) {
        this.config = res.value;
      }
    });

    this.gamificationService.getClasses(this.courseId, this.periodId).subscribe((res) => {
      this.loading = false;
      if (res.success && res.value) {
        this.classGroups = res.value;
      }
    });
  }

  public saveConfig(): void {
    if (!this.config || this.courseId <= 0) return;
    this.gamificationService.updateConfig(this.courseId, this.config).subscribe((res) => {
      if (res.success && res.value) {
        this.config = res.value;
        this.toastr.success("Gamification configuration saved.", "Success");
        this.configChanged.emit(res.value);
      } else {
        this.toastr.error(res.message || "Failed to update configuration.", "Error");
      }
    });
  }

  public addClass(): void {
    if (!this.newClassName?.trim() || this.courseId <= 0) return;
    this.gamificationService.addClass(this.courseId, this.newClassName.trim(), this.periodId).subscribe((res) => {
      if (res.success && res.value) {
        this.classGroups = [...this.classGroups, res.value];
        this.newClassName = "";
        this.toastr.success("Class group added.", "Success");
        this.classesChanged.emit();
      } else {
        this.toastr.error(res.message || "Failed to add class.", "Error");
      }
    });
  }

  public deleteClass(classGroupId: number): void {
    if (!confirm("Are you sure you want to delete this class group?")) return;
    this.gamificationService.deleteClass(this.courseId, classGroupId).subscribe((res) => {
      if (res.success) {
        this.classGroups = this.classGroups.filter((g) => g.id !== classGroupId);
        this.toastr.success("Class group deleted.", "Success");
        this.classesChanged.emit();
      } else {
        this.toastr.error(res.message || "Failed to delete class.", "Error");
      }
    });
  }
}

