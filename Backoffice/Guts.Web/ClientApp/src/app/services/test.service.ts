import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { map, catchError } from "rxjs/operators";
import { PostResult } from "../util/result";


@Injectable()
export class TestService {

  constructor(private http: HttpClient) { }

  public deleteTest(testId: number): Observable<PostResult> {
    return this.http.delete('api/tests/' + testId, {})
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }
}
