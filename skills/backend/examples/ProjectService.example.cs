using DA.KinHub.Business.Projects;

// Risolvere IProjectService da DI; non istanziare repository infrastrutturali nel chiamante.
ProjectDto project = await service.CreateAsync(new CreateProjectRequest("Menu settimanale"), cancellationToken);
