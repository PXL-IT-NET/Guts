import { Component, OnInit, Input } from '@angular/core';
import { IExamPartModel, ExamPartModel } from '../../viewmodels/exam.model';

@Component({
  selector: 'app-exampart',
  templateUrl: './exampart.component.html'
})
export class ExampartComponent implements OnInit {

  @Input() public model: IExamPartModel;

  public isCollapsed: boolean;

  constructor() {
    this.isCollapsed = true;
   }

  ngOnInit() {
    if(!this.model){
      this.model = new ExamPartModel();
    }
  }

  public saveExampart(){
    console.log('Saving exampart');
    console.log(this.model);
  }
}
