import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { map, catchError } from "rxjs/operators";
import { GetResult, PostResult } from "../util/result";
import { IPeriodModel, PeriodModel } from "../viewmodels/period.model";

export interface ISavePeriodRequest {
  description: string;
  from: string;
  until: string;
}

@Injectable({
  providedIn: "root",
})
export class PeriodService {
  constructor(private readonly http: HttpClient) {}

  public getAll(): Observable<GetResult<PeriodModel[]>> {
    return this.http.get<IPeriodModel[]>("api/periods").pipe(
      map((periodModels) =>
        GetResult.success(periodModels.map((model) => new PeriodModel(model))),
      ),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(
          GetResult.fromHttpErrorResponse<PeriodModel[]>(errorResponse),
        );
      }),
    );
  }

  public createPeriod(
    model: ISavePeriodRequest,
  ): Observable<GetResult<PeriodModel>> {
    return this.http.post<IPeriodModel>("api/periods", model).pipe(
      map((created) => GetResult.success(new PeriodModel(created))),
      catchError((errorResponse: HttpErrorResponse) =>
        of(GetResult.fromHttpErrorResponse<PeriodModel>(errorResponse)),
      ),
    );
  }

  public updatePeriod(
    periodId: number,
    model: ISavePeriodRequest,
  ): Observable<PostResult> {
    return this.http.put(`api/periods/${periodId}`, model).pipe(
      map(() => PostResult.success()),
      catchError((errorResponse: HttpErrorResponse) =>
        of(PostResult.fromHttpErrorResponse(errorResponse)),
      ),
    );
  }

  public deletePeriod(periodId: number): Observable<PostResult> {
    return this.http.delete(`api/periods/${periodId}`).pipe(
      map(() => PostResult.success()),
      catchError((errorResponse: HttpErrorResponse) =>
        of(PostResult.fromHttpErrorResponse(errorResponse)),
      ),
    );
  }
}
