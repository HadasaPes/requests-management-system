import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/problem-details.model';

export interface ErrorMessage {
  message: string;
  /** Field validation messages (400), if any. */
  details: string[];
}

/** Turns an HTTP error into a message for the user, taken from the API's ProblemDetails when possible. */
export function toErrorMessage(error: unknown): ErrorMessage {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return { message: 'Cannot reach the server. Check that the API is running, then try again.', details: [] };
    }

    const problem = error.error as ProblemDetails | null;
    if (problem?.detail || problem?.title) {
      return {
        message: problem.detail ?? problem.title!,
        details: problem.errors ? Object.values(problem.errors).flat() : [],
      };
    }
  }

  return { message: 'Something went wrong. Please try again.', details: [] };
}
