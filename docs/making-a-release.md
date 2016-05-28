Steps to make a new release
===========================

1. Make sure no one else is planning on doing anything that would trigger a build
2. Check the "next build number" on [AppVeyor](https://ci.appveyor.com/project/Pilchie/xunit-runner-wpf/settings)
3. Click on [releases](https://github.com/Pilchie/xunit.runner.wpf/releases) -> `Draft a new release`
4. Set the version to `v1.0.nextbuildnumber` from 2
5. Set the title to `v1.0.nextbuildnumber - Some reason for the release to exist`
6. Click `Publish`
7. This will create the release, and start a new build on AppVeyor
8. Download the .nupkg from the AppVeyor artifacts page for that new build - (e.g. https://ci.appveyor.com/project/Pilchie/xunit-runner-wpf/build/1.0.15/artifacts)
9. Go back to the release you created in 6, and add the nupkg, and write a changelog
10. Tell [@Pilchie](https://github.com/Pilchie) to upload the nupkg to NuGet.org
