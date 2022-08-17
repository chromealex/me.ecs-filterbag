namespace ME.ECS {

    public static class ForEachUtils {

        public unsafe delegate void InternalDelegate(void* fn, void* bagPtr);
        public unsafe delegate void InternalParallelForDelegate(void* fn, void* bagPtr, int index);

        public struct ForEachTask<T> {
            
            public delegate void ForEachTaskDelegate(in ForEachTask<T> task, in Filter filter, T callback);

            internal bool withBurst;
            internal bool parallelFor;
            internal int batchCount;
            private ForEachTaskDelegate task;
            private Filter filter;
            private T callback;

            public ForEachTask(in Filter filter, T callback, ForEachTaskDelegate task) {

                this.task = task;
                this.withBurst = false;
                this.filter = filter;
                this.callback = callback;

                this.parallelFor = false;
                this.batchCount = 64;

            }

            public ForEachTask<T> WithBurst() {

                this.withBurst = true;
                return this;

            }

            public ForEachTask<T> ParallelFor(int batchCount = 64) {

                this.parallelFor = true;
                this.batchCount = batchCount;
                return this;

            }

            public void Do() {
                
                this.task.Invoke(in this, in this.filter, this.callback);
                
            }

        }

    }

}