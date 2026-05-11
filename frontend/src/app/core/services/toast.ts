import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ToastMessage {
  id: number;
  text: string;
  type: 'success' | 'error';
}

@Injectable({ providedIn: 'root' })
export class Toast {
  private nextId = 1;
  private readonly messagesSubject = new BehaviorSubject<ToastMessage[]>([]);
  readonly messages$ = this.messagesSubject.asObservable();

  success(text: string): void {
    this.push(text, 'success');
  }

  error(text: string): void {
    this.push(text, 'error');
  }

  dismiss(id: number): void {
    this.messagesSubject.next(this.messagesSubject.value.filter(message => message.id !== id));
  }

  private push(text: string, type: ToastMessage['type']): void {
    const message = { id: this.nextId++, text, type };
    this.messagesSubject.next([...this.messagesSubject.value, message]);
    window.setTimeout(() => this.dismiss(message.id), 3200);
  }
}
