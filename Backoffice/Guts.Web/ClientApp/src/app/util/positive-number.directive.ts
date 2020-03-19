import { NG_VALIDATORS, Validator, Validators, AbstractControl } from '@angular/forms';
import { Directive } from '@angular/core';

@Directive({
    selector: '[positive-number]',
    providers: [{provide: NG_VALIDATORS, useExisting: PositiveNumberValidatorDirective, multi: true}]
  })
  export class PositiveNumberValidatorDirective implements Validator {

    validate(control: AbstractControl): {[key: string]: any} | null {
        return Validators.min(1)(control);
    }
  }