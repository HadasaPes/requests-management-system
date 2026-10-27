# Known limitations

## No authentication: changedBy is not verified

The app has no login. The user types a name in the "Your name" field at the top of the page. It is kept in memory only
(`CurrentUserService`, lost on reload) and sent as `changedBy` with every status change. The API checks only that it
is present and at most 100 characters; anyone can send any name.

So the "changed by" value in the history shows who the user *said* they were, not who they are.

**With authentication:** the API would take the user from the authenticated identity (for example the `name` claim of
a JWT), ignore or remove `changedBy` from the request body, and could also restrict who may change statuses.
