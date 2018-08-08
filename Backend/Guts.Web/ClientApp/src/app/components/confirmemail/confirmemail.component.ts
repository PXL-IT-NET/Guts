import { Component, OnInit } from '@angular/core';
import { Params, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';


@Component({
    templateUrl: './confirmemail.component.html'
})
export class ConfirmEmailComponent implements OnInit {

    public message: string;
    public confirmed: boolean;
    public loading: boolean;

    constructor(
        private activatedRoute: ActivatedRoute,
        private authenticationService: AuthService) {
        this.message = 'Confirming email...';
        this.confirmed = false;
        this.loading = true;
    }

    ngOnInit() {
        this.activatedRoute.queryParams.subscribe((params: Params) => {
            let model = {
                userId: params['userId'],
                token: params['token']
            };

            this.authenticationService.confirmEmail(model)
                .subscribe(result => {
                    this.confirmed = result.success;
                    if (this.confirmed) {
                        this.message = "Your email address has been succesfully confirmed. Your registration is complete.";
                    } else {
                        this.message = result.message || 'Something went wrong while validating your email address.';
                    }
                    this.loading = false;
                });
        });
    }
}