import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { GetResult } from "../util/result";
import { IPeriodModel, PeriodModel } from '../viewmodels/period.model';

@Injectable()
export class PeriodService {

  constructor(private http: HttpClient) { }

  public getAll(): Observable<GetResult<PeriodModel[]>> {
    return this.http.get<IPeriodModel[]>('api/periods')
      .pipe(
        map(periodModels => GetResult.success(periodModels.map(model => new PeriodModel(model)))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<PeriodModel[]>(errorResponse));
        })
      );
  }
}