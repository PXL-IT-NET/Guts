import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { GetResult } from "../util/result";

export interface IQuartzDashboardTicketModel {
  loginUrl: string;
}

@Injectable({
  providedIn: "root",
})
export class ScheduledJobsService {
  constructor(private readonly http: HttpClient) {}

  public createDashboardTicket(): Observable<
    GetResult<IQuartzDashboardTicketModel>
  > {
    return this.http
      .post<IQuartzDashboardTicketModel>("api/scheduled-jobs/dashboard-ticket", {})
      .pipe(
        map((model) => GetResult.success<IQuartzDashboardTicketModel>(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(
            GetResult.fromHttpErrorResponse<IQuartzDashboardTicketModel>(
              errorResponse,
            ),
          );
        }),
      );
  }
}
