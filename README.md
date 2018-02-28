# Guts
GUTS stands for **G**rowing towards a **U**nit **T**esting **S**trategy.
With GUTS we try to provide tools for teachers and students to **integrate automated testing into courses that involve writing code in a programming language**.

## General idea
When students work on programming exercises, the **exercises** are **accompanied with automated tests** that check the correctness of the solution of the students.
The students get immediate feedback about the mistakes they made.
Each time the student runs automated tests, the **results** of that testrun **are sent to a [central service](Backend/README.md)** (Rest API) that collects data.

On a central dashboard the students can check their progress and compare his/her results with the average.
Teachers can get more insights into the progress of their students and detect problem area's.

## Goals
- Develop components and tools that help teachers to compose automated tests for exercises for the different programming languages.
  - .NET
    - For .NET some client components and tools exist. The code and other information can be found in the [DotNetClient](DotNetClient/README.md) folder.
  - Java
    - *Todo*
  - Other programming languages
    - *Todo*
- Stimulate students to make more exercises thanks to the automated feeback of automated tests.
- Develop a [web portal](Backend/README.md) (backend) that provides insights into the learning process of students with regard to the number and frequency of the exercises that are made.
- Integration of automated tests in all courses that involve writing code in a programming language.

## Colleges
This project was set up by university college [PXL](http://www.pxl.be), but other colleges and universities are welcome to contribute to this project.


