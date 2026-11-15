export interface MockUserProfile {
  id: number;
  email: string;
  roles: string[];
  teams: number[];
  isAuthenticated: boolean;
}

export const mockTeacherProfile: MockUserProfile = {
  id: 101,
  email: "teacher.guts@pxl.be",
  roles: ["lector"],
  teams: [],
  isAuthenticated: true,
};

export const mockStudentProfile: MockUserProfile = {
  id: 202,
  email: "student.guts@student.pxl.be",
  roles: ["student"],
  teams: [1, 2],
  isAuthenticated: true,
};

export const mockAdminProfile: MockUserProfile = {
  id: 1,
  email: "admin.guts@pxl.be",
  roles: ["lector", "admin"],
  teams: [],
  isAuthenticated: true,
};

