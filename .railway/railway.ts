import { defineRailway, github, project, service } from "railway/iac";

export default defineRailway(() => {
  const api = service("api", {
    source: github("BautistaConta/Gym-Manager", { branch: "main" }),
    healthcheck: "/health/ready",
    healthcheckTimeout: 120,
    replicas: { "us-east4-eqdc4a": 1 },
  });

  return project("gym-manager", { resources: [api] });
});
