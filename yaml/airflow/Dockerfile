FROM apache/spark AS spark
FROM apache/airflow:2.10.2
COPY --from=spark /opt/java/openjdk /opt/java/openjdk
COPY --from=spark /opt/spark /opt/spark

# Install JDK
USER root
ENV JAVA_HOME=/opt/java/openjdk
ENV SPARK_HOME=/opt/spark
ENV PATH=$PATH:$JAVA_HOME/bin