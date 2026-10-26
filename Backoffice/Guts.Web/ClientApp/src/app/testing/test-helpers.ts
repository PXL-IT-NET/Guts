import { ActivatedRoute, Params, Router } from "@angular/router";
import { BsModalRef, BsModalService } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { BehaviorSubject, Subject } from "rxjs";

export class ActivatedRouteStub {
  private readonly paramsSubject = new BehaviorSubject<Params>({});
  private readonly queryParamsSubject = new BehaviorSubject<Params>({});

  public params = this.paramsSubject.asObservable();
  public queryParams = this.queryParamsSubject.asObservable();

  public snapshot: { params: Params; queryParams: Params } = {
    params: {},
    queryParams: {},
  };

  public parent: { snapshot: { params: Params } } = {
    snapshot: { params: {} },
  };

  constructor(
    initialParams: Params = {},
    initialQueryParams: Params = {},
    parentRoute?: ActivatedRouteStub,
  ) {
    this.setParams(initialParams);
    this.setQueryParams(initialQueryParams);
    if (parentRoute) {
      this.parent = parentRoute;
    }
  }

  public setParams(params: Params): void {
    this.snapshot.params = params;
    this.paramsSubject.next(params);
  }

  public setQueryParams(queryParams: Params): void {
    this.snapshot.queryParams = queryParams;
    this.queryParamsSubject.next(queryParams);
  }

  public setParentParams(params: Params): void {
    this.parent.snapshot.params = params;
  }
}

export function createActivatedRouteProvider(
  initialParams: Params = {},
  initialParentParams: Params = {},
  initialQueryParams: Params = {},
): { provide: typeof ActivatedRoute; useValue: ActivatedRouteStub } {
  const stub = new ActivatedRouteStub();
  stub.setParentParams(initialParentParams);
  stub.setParams(initialParams);
  stub.setQueryParams(initialQueryParams);
  return { provide: ActivatedRoute, useValue: stub };
}

export function createToastrSpy(): jasmine.SpyObj<ToastrService> {
  return jasmine.createSpyObj<ToastrService>("ToastrService", [
    "success",
    "error",
    "warning",
    "info",
  ]);
}

export function createRouterSpy(initialUrl = "/"): jasmine.SpyObj<Router> & {
  url: string;
  events$: Subject<any>;
} {
  const events$ = new Subject<any>();
  const spy = jasmine.createSpyObj<Router>("Router", ["navigate"], {
    events: events$.asObservable(),
  }) as any;
  spy.url = initialUrl;
  spy.events$ = events$;
  return spy;
}

export function createModalRefSpy<T = any>(
  content: T = {} as T,
): jasmine.SpyObj<BsModalRef> {
  const modalRef = jasmine.createSpyObj<BsModalRef>("BsModalRef", [
    "hide",
    "setClass",
  ]);
  modalRef.content = content;
  return modalRef;
}

export function createModalServiceSpy(
  modalRef: BsModalRef = createModalRefSpy(),
): jasmine.SpyObj<BsModalService> {
  const service = jasmine.createSpyObj<BsModalService>("BsModalService", [
    "show",
  ]);
  service.show.and.returnValue(modalRef);
  return service;
}

