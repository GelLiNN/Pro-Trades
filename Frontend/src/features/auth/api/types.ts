export interface SessionResponse {
  authToken: string
  email: string
  isLoggedIn: boolean
  userId: string
  username: string
  userTypeId: number
}

export interface RawGetUserResponse extends SessionResponse {}

export interface RawLoginResponse extends SessionResponse {}

export interface RawRegisterResponse extends SessionResponse {}
